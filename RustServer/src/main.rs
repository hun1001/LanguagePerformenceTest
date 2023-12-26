use tokio::sync::mpsc::{channel, Sender};
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::tcp::OwnedWriteHalf;
use tokio::net::{TcpListener, TcpStream};
use bytes::{Bytes, BytesMut};

#[derive(Debug)]
struct Packet {
    user_id: String,
    time_stamp: String,
    message: String,
}

impl Packet {
    fn serialize(&self) -> Bytes {
        let mut buf = BytesMut::new();
        buf.extend_from_slice(self.user_id.as_bytes());
        buf.extend_from_slice("|".as_bytes());
        buf.extend_from_slice(self.time_stamp.as_bytes());
        buf.extend_from_slice("|".as_bytes());
        buf.extend_from_slice(self.message.as_bytes());
        buf.freeze()
    }

    fn deserialize(buf: &[u8]) -> Self {
        let mut user_id = String::new();
        let mut time_stamp = String::new();
        let mut message = String::new();

        let string = String::from_utf8_lossy(buf);

        let iter: Vec<&str> = string.split('|').collect();

        if iter.len() != 3 {
            return Self {
                user_id: String::new(),
                time_stamp: String::new(),
                message: String::new(),
            };
        }

        user_id.push_str(iter[0]);
        time_stamp.push_str(iter[1]);
        message.push_str(iter[2]);

        Self {
            user_id,
            time_stamp,
            message,
        }
    }
}

enum Message {
    Packet(Packet),
    Failed(i32),
}

struct Client {
    id: i32,
    writer: OwnedWriteHalf,
}

async fn accept_loop(tcp: TcpListener, socket_sender: Sender<TcpStream>) {
    loop {
        let socket = match tcp.accept().await {
            Ok((socket, _)) => socket,
            Err(e) => panic!("Failed to accept socket: {}", e),
        };

        if let Err(e) = socket_sender.send(socket).await {
            println!("Failed to send socket: {}", e);
        }
    }
}

fn handle_new_connection(writers: &mut Vec<Client>, sender: Sender<Message>, socket: TcpStream, id: i32) {
    let (mut reader, writer) = socket.into_split();

    let client = Client {
        id,
        writer,
    };

    writers.push(client);

    println!("New connection: {}", id);

    tokio::spawn(async move {
        let mut buf = [0; 1024];
        loop {
            let size = match reader.read(&mut buf).await {
                Ok(n) => {
                    if n == 0 {
                        return;
                    }
                    n
                },
                Err(e) => {
                    println!("Failed to read from socket {}: {}", id, e);
                    if let Err(e) = sender.send(Message::Failed(id)).await {
                        println!("Failed to send failed message: {}", e);
                    }
                    return;
                }
            };

            if size == 0 {
                return;
            }

            let packet = Packet::deserialize(&buf);

            if let Err(e) = sender.send(Message::Packet(packet)).await {
                println!("Failed to send message: {}", e);
                return;
            }
        }
    });
}

async fn handle_message(writers: &mut Vec<Client>, packet: &Message) {
    let mut clients_for_removal: Vec<i32> = Vec::new();

    for writer in writers.iter_mut() {
        match packet {
            Message::Packet(packet) => {
                println!("Received packet: {:?}", packet);

                if let Err(e) = writer.writer.write_all(&packet.serialize()).await {
                    println!("Failed to write to socket: {}", e);
                }

                if let Err(e) = writer.writer.flush().await {
                    println!("Failed to flush socket: {}", e);
                }
            }
            Message::Failed(id) => {
                if writer.id == *id {
                    clients_for_removal.push(*id);
                }
            }
        }
    }

    for id in clients_for_removal {
        writers.retain(|client| client.id != id);
    }
}

#[tokio::main]
async fn main() {
    let tcp = match TcpListener::bind("127.0.0.1:4444").await {
        Ok(tcp) => tcp,
        Err(e) => panic!("Failed to bind to port 4444: {}", e),
    };

    println!("Listening on port 4444");

    let mut writers: Vec<Client> = Vec::new();

    let (packet_sender, mut packet_receiver) = channel::<Message>(100);
    let (socket_sender, mut socket_receiver) = channel::<TcpStream>(100);

    tokio::spawn(accept_loop(tcp, socket_sender));

    let mut id = 0;

    loop {
        tokio::select! {
            Some(socket) = socket_receiver.recv() => {
                id += 1;
                handle_new_connection(&mut writers, packet_sender.clone(), socket, id);
            }
            Some(packet) = packet_receiver.recv() => {
                handle_message(&mut writers, &packet).await;
            }
        }
    }
}
