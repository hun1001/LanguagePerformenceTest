use bytes::BytesMut;
use log::{debug, error, info};
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::tcp::{OwnedReadHalf, OwnedWriteHalf};
use tokio::net::TcpStream;
use tokio::sync::mpsc::{channel, Sender};

use crate::packet::Packet;

mod packet;
mod packet_factory;

struct Client {
    writer: OwnedWriteHalf,
}

#[derive(Debug)]
struct ClientMessage {
    id: usize,
    message: Message,
}

#[derive(Debug)]
enum Message {
    Packet(Packet),
    Disconnect,
}

async fn handle_client(id: usize, mut reader: OwnedReadHalf, packet_tx: Sender<ClientMessage>) {
    let mut packet_factory = packet_factory::PacketFactory::new();
    let mut read_buf = [0; 1024];

    loop {
        if let Ok(n) = reader.read(&mut read_buf).await {
            if n == 0 {
                packet_tx.send(ClientMessage {
                    id,
                    message: Message::Disconnect,
                }).await.unwrap();
                break;
            }

            packet_factory.push(&read_buf[..n]);

            while let Some(packet) = packet_factory.next() {
                if packet_tx.send(ClientMessage {
                    id,
                    message: Message::Packet(packet),
                }).await.is_err() {
                    packet_tx.send(ClientMessage {
                        id,
                        message: Message::Disconnect,
                    }).await.unwrap();

                    break;
                }
            }
        }
    }
}

async fn run_server(port: u16) {
    let server = match tokio::net::TcpListener::bind(format!("127.0.0.1:{}", port)).await {
        Ok(server) => server,
        Err(e) => {
            error!("Failed to bind: {}", e);
            return;
        }
    };

    let mut clients: Vec<Option<Client>> = Vec::new();

    info!("Listening on: {}", server.local_addr().unwrap());

    let (stream_tx, mut stream_rx) = channel::<TcpStream>(1024);
    let (message_tx, mut message_rx) = channel::<ClientMessage>(1024);

    tokio::spawn(async move {
        loop {
            let stream = match server.accept().await {
                Ok((stream, _)) => stream,
                Err(e) => {
                    error!("Failed to accept: {}", e);
                    continue;
                }
            };

            debug!("Accepted connection from: {}", stream.peer_addr().unwrap());
            stream_tx.send(stream).await.expect("Failed to send stream");
        }
    });

    loop {
        let mut disconnected_clients = Vec::new();

        tokio::select! {
            Some(stream) = stream_rx.recv() => {
                let (read, write) = stream.into_split();

                for (id, client) in clients.iter().enumerate() {
                    if client.is_none() {
                        clients[id] = Some(Client {
                            writer: write,
                        });
                        tokio::spawn(handle_client(id, read, message_tx.clone()));
                        return;
                    }
                }

                clients.push(Some(Client {
                    writer: write,
                }));

                tokio::spawn(handle_client(clients.len() - 1, read, message_tx.clone()));
            },
            Some(message) = message_rx.recv() => {
                match message.message {
                    Message::Packet(packet) => {
                        for client in clients.iter_mut() {
                            let packet = packet.serialize();

                            let mut buf = BytesMut::new();
                            buf.extend_from_slice(&packet);

                            match client {
                                Some(client) => {
                                    if let Err(e) = client.writer.write_all(&buf).await {
                                        debug!("Connection closed: {}", e);
                                        break;
                                    }
                                },
                                None => {
                                    debug!("Client is None");
                                }
                            }
                        }
                    },
                    Message::Disconnect => {
                        debug!("Client disconnected");
                        disconnected_clients.push(message.id);
                        break;
                    }
                }
            }
        }

        for id in disconnected_clients {
            clients[id] = None;
        }
    }
}

#[tokio::main]
async fn main() {
    env_logger::init_from_env(env_logger::Env::default().default_filter_or("debug"));
    info!("Logger initialized. Level: {}", log::max_level());

    run_server(4444).await;
}

#[cfg(test)]
mod tests {
    use std::sync::Once;

    use super::*;

    static INIT: Once = Once::new();

    fn init() {
        INIT.call_once(|| {
            env_logger::init_from_env(env_logger::Env::default().default_filter_or("debug"));
        });
    }

    async fn send_packet(stream: &mut TcpStream, packet: &Packet) {
        let packet_buf = packet.serialize();

        let mut buf = BytesMut::new();
        buf.extend_from_slice(&packet_buf);

        stream.write_all(&buf).await.unwrap();
    }

    async fn read_packet(stream: &mut TcpStream, factory: &mut packet_factory::PacketFactory) -> Packet {
        let mut buf = [0; 1024];

        loop {
            let n = stream.read(&mut buf).await.unwrap();

            factory.push(&buf[..n]);

            if let Some(packet) = factory.next() {
                return packet;
            }
        }
    }

    #[tokio::test]
    async fn test_server() {
        init();
        tokio::spawn(run_server(44445));

        let mut stream = TcpStream::connect("127.0.0.1:44445").await.unwrap();

        let packet = Packet {
            user_id: String::from("test"),
            time_stamp: String::from("2021-01-01 00:00:00"),
            message: String::from("Hello, world!"),
        };

        send_packet(&mut stream, &packet).await;

        let mut factory = packet_factory::PacketFactory::new();
        let packet = read_packet(&mut stream, &mut factory).await;

        assert_eq!(packet.user_id, "test");
    }

    #[tokio::test]
    async fn test_server_100() {
        init();
        tokio::spawn(run_server(44447));

        let mut stream = TcpStream::connect("127.0.0.1:44447").await.unwrap();
        let mut factory = packet_factory::PacketFactory::new();

        for _ in 0..100 {
            let packet = Packet {
                user_id: String::from("test"),
                time_stamp: String::from("2021-01-01 00:00:00"),
                message: String::from("Hello, world!"),
            };

            send_packet(&mut stream, &packet).await;

            let packet = read_packet(&mut stream, &mut factory).await;

            assert_eq!(packet.user_id, "test");
        }
    }

    #[tokio::test]
    async fn test_server_multiple() {
        init();
        tokio::spawn(run_server(44446));

        const CLIENT_COUNT: usize = 100;
        const PACKET_COUNT: usize = 100;

        let mut handles = Vec::new();

        for i in 0..CLIENT_COUNT {
            let handle = tokio::spawn(async move {
                let mut stream = TcpStream::connect("127.0.0.1:44446").await.unwrap();
                let mut factory = packet_factory::PacketFactory::new();

                let mut packet_num = i * PACKET_COUNT;

                for _ in 0..PACKET_COUNT {
                    let packet = Packet {
                        user_id: String::from("test"),
                        time_stamp: String::from("2021-01-01 00:00:00"),
                        message: format!("Hello, world! {}", packet_num),
                    };

                    send_packet(&mut stream, &packet).await;

                    if packet_num < 100 {
                        debug!("Sending packet {} from {}", packet_num, i);
                    }

                    loop {
                        let packet = read_packet(&mut stream, &mut factory).await;

                        if packet.message == "Hello, world! 1" {
                            debug!("Received packet {} from {}", packet.message, i);
                        }

                        if packet.message == format!("Hello, world! {}", packet_num) {
                            break;
                        }
                    }

                    if packet_num < 100 {
                        debug!("Packet {} Confirmed from {}", packet_num, i);
                    }

                    packet_num += 1;
                }

                debug!("Client {} finished", i);
            });

            handles.push(handle);
        }

        for handle in handles {
            handle.await.unwrap();
        }
    }
}
