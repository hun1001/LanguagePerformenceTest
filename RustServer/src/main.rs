use tokio::sync::mpsc::{channel, Sender};
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::tcp::OwnedWriteHalf;
use tokio::net::{TcpListener, TcpStream};
use bytes::{Bytes, BytesMut};
use log::{debug, error, info};

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
            error!("Failed to send socket: {}", e);
        }
    }
}

fn handle_new_connection(writers: &mut Vec<Client>, sender: Sender<Packet>, dead_sender: Sender<i32>, socket: TcpStream, id: i32) {
    let (mut reader, writer) = socket.into_split();

    let client = Client {
        id,
        writer,
    };

    writers.push(client);

    debug!("New connection: {}", id);

    tokio::spawn(async move {
        let mut buf = [0; 1024];
        loop {
            let size = match reader.read(&mut buf).await {
                Ok(n) => {
                    if n == 0 {
                        return;
                    }
                    n
                }
                Err(e) => {
                    error!("Failed to read from socket {}: {}", id, e);
                    if let Err(e) = dead_sender.send(id).await {
                        error!("Failed to send failed message: {}", e);
                    }
                    return;
                }
            };

            if size == 0 {
                return;
            }

            let packet = Packet::deserialize(&buf);

            if let Err(e) = sender.send(packet).await {
                error!("Failed to send message: {}", e);
                return;
            }
        }
    });
}

async fn handle_message(writers: &mut Vec<Client>, packet: &Packet) {
    debug!("Message: {:?}", packet);

    for writer in writers.iter_mut() {
        if let Err(e) = writer.writer.write_all(&packet.serialize()).await {
            error!("Failed to write to socket: {}", e);
        }
    }
}

#[tokio::main]
async fn main() {
    env_logger::init();

    let tcp = match TcpListener::bind("127.0.0.1:4444").await {
        Ok(tcp) => tcp,
        Err(e) => panic!("Failed to bind to port 4444: {}", e),
    };

    info!("Listening on port 4444");

    let mut writers: Vec<Client> = Vec::new();

    let (packet_sender, mut packet_receiver) = channel::<Packet>(1000);
    let (socket_sender, mut socket_receiver) = channel::<TcpStream>(1000);
    let (dead_sender, mut dead_receiver) = channel::<i32>(1000);

    tokio::spawn(accept_loop(tcp, socket_sender));

    let mut id = 0;

    loop {
        tokio::select! {
            Some(socket) = socket_receiver.recv() => {
                id += 1;
                handle_new_connection(&mut writers, packet_sender.clone(), dead_sender.clone(), socket, id);
            }
            Some(packet) = packet_receiver.recv() => {
                tokio::spawn(handle_message(&mut writers, &packet));
            }
            Some(id) = dead_receiver.recv() => {
                writers.retain(|client| client.id != id);
            }
        }
    }
}
