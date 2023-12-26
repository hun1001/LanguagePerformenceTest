use std::net::IpAddr;
use crossbeam_channel::unbounded;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::tcp::OwnedWriteHalf;
use tokio::net::{TcpListener, TcpStream};

#[derive(Debug)]
struct Packet {
    user_id: String,
    time_stamp: String,
    message: String,
}

impl Packet {
    fn serialize(&self) -> Vec<u8> {
        let mut buf = Vec::new();
        buf.extend_from_slice(self.user_id.as_bytes());
        buf.extend_from_slice("|".as_bytes());
        buf.extend_from_slice(self.time_stamp.as_bytes());
        buf.extend_from_slice("|".as_bytes());
        buf.extend_from_slice(self.message.as_bytes());
        buf
    }

    fn deserialize(buf: &[u8]) -> Self {
        let mut user_id = String::new();
        let mut time_stamp = String::new();
        let mut message = String::new();

        let string = String::from_utf8_lossy(buf);

        let iter: Vec<&str> = string.split("|").collect();

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

#[tokio::main]
async fn main() {
    let tcp = match TcpListener::bind("127.0.0.1:4444").await {
        Ok(tcp) => tcp,
        Err(e) => panic!("Failed to bind to port 4444: {}", e),
    };

    println!("Listening on port 4444");

    let mut writers: Vec<OwnedWriteHalf> = Vec::new();

    let (sender, receiver) = unbounded::<Packet>();
    let (socket_sender, socket_receiver) = unbounded::<TcpStream>();

    tokio::spawn(async move {
        loop {
            let socket = match tcp.accept().await {
                Ok((socket, _)) => socket,
                Err(e) => panic!("Failed to accept socket: {}", e),
            };

            println!("New client connected");

            if let Err(e) = socket_sender.send(socket) {
                println!("Failed to send socket: {}", e);
            }
        }
    });

    loop {
        if let Ok(socket) = socket_receiver.try_recv() {
            let (mut reader, writer) = socket.into_split();
            let sender = sender.clone();
            writers.push(writer);

            tokio::spawn(async move {
                let mut buf = [0; 1024];
                loop {
                    let n = match reader.read(&mut buf).await {
                        Ok(n) if n == 0 => {
                            println!("Client disconnected");
                            return;
                        }
                        Ok(n) => n,
                        Err(e) => {
                            println!("Failed to read from socket: {}", e);
                            return;
                        }
                    };
                    if n == 0 {
                        return;
                    }
                    let packet = Packet::deserialize(&buf);

                    if let Err(e) = sender.send(packet) {
                        println!("Failed to send packet: {}", e);
                    }
                }
            });
        }

        if let Ok(packet) = receiver.try_recv() {
            for writer in writers.iter_mut() {
                if let Err(e) = writer.write_all(&packet.serialize()).await {
                    println!("Failed to write to socket: {}", e);
                }
            }
        }
    }
}
