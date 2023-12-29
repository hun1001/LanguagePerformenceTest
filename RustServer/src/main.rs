use bytes::{Bytes, BytesMut};
use log::{debug, error, info};
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::TcpStream;
use tokio::sync::broadcast;
use tokio::sync::broadcast::Sender;

#[derive(Debug, Clone)]
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
            error!("Invalid packet: {}", string);
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

async fn handle_client(mut stream: TcpStream, tx: Sender<Packet>) {
    let mut buf = [0; 1024];
    let mut buf_len = 0;
    let mut next_packet_size = 0;
    let mut is_next_packet = true;

    let mut read_buf = [0; 1024];

    let (mut read, mut write) = stream.split();
    let mut rx = tx.subscribe();

    loop {
        tokio::select! {
            Ok(n) = read.read(&mut read_buf) => {
                if n == 0 {
                    error!("Connection closed");
                    break;
                }

                buf[buf_len..buf_len + n].copy_from_slice(&read_buf[..n]);

                debug!("Buffer length: {} + {}", buf_len, n);

                buf_len += n;

                loop {
                    if is_next_packet {
                        if buf_len < 4 {
                            break;
                        }

                        next_packet_size = i32::from_be_bytes([buf[0], buf[1], buf[2], buf[3]]) as usize;
                        buf_len -= 4;

                        buf.copy_within(4..4 + buf_len, 0);

                        debug!("Next packet size: {}", next_packet_size);
                    }

                    if buf_len < next_packet_size {
                        is_next_packet = false;
                        debug!("Not enough data: {} < {}", buf_len, next_packet_size);
                        break;
                    }

                    buf_len -= next_packet_size;

                    let packet = Packet::deserialize(&buf[..next_packet_size]);

                    buf.copy_within(next_packet_size.., 0);

                    debug!("Received packet: {:?}", packet);

                    if let Err(e) = tx.send(packet) {
                        error!("Failed to send packet: {}", e);
                        break;
                    }

                    is_next_packet = true;
                }
            }
            Ok(packet) = rx.recv() => {
                debug!("Sending packet: {:?}", packet);

                let packet = packet.serialize();
                let packet_size = (packet.len() as i32).to_be_bytes();

                let mut buf = BytesMut::new();
                buf.extend_from_slice(&packet_size);
                buf.extend_from_slice(&packet);

                if let Err(e) = write.write_all(&buf).await {
                    error!("Failed to write to socket: {}", e);
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

    info!("Listening on: {}", server.local_addr().unwrap());

    let (tx, _) = broadcast::channel::<Packet>(128);

    loop {
        let (stream, _) = server.accept().await.unwrap();
        debug!("Accepted connection from: {}", stream.peer_addr().unwrap());
        tokio::spawn(handle_client(stream, tx.clone()));
    }
}

#[tokio::main]
async fn main() {
    env_logger::init_from_env(env_logger::Env::default().default_filter_or("info"));
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

    #[test]
    fn test_packet_serialize() {
        let packet = Packet {
            user_id: String::from("test"),
            time_stamp: String::from("2021-01-01 00:00:00"),
            message: String::from("Hello, world!"),
        };

        let buf = packet.serialize();

        assert_eq!(
            String::from_utf8_lossy(&buf),
            "test|2021-01-01 00:00:00|Hello, world!"
        );
    }

    #[test]
    fn test_packet_deserialize() {
        let buf = String::from("test|2021-01-01 00:00:00|Hello, world!").into_bytes();

        let packet = Packet::deserialize(&buf);

        assert_eq!(packet.user_id, "test");
        assert_eq!(packet.time_stamp, "2021-01-01 00:00:00");
        assert_eq!(packet.message, "Hello, world!");
    }

    #[test]
    fn test_packet_deserialize_invalid() {
        let buf = String::from("test|2021-01-01 00:00:00").into_bytes();

        let packet = Packet::deserialize(&buf);

        assert_eq!(packet.user_id, "");
        assert_eq!(packet.time_stamp, "");
        assert_eq!(packet.message, "");
    }

    #[test]
    fn test_packet_deserialize_empty() {
        let buf = String::from("").into_bytes();

        let packet = Packet::deserialize(&buf);

        assert_eq!(packet.user_id, "");
        assert_eq!(packet.time_stamp, "");
        assert_eq!(packet.message, "");
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

        let packet_buf = packet.serialize();
        let packet_size = (packet_buf.len() as i32).to_be_bytes();

        let mut buf = BytesMut::new();
        buf.extend_from_slice(&packet_size);
        buf.extend_from_slice(&packet_buf);

        stream.write_all(&buf).await.unwrap();

        let mut buf = [0; 4];
        _ = stream.read(&mut buf).await.unwrap();
        let packet_size = i32::from_be_bytes([buf[0], buf[1], buf[2], buf[3]]) as usize;
        debug!("Packet size: {}", packet_size);

        let mut buf = vec![0; packet_size];
        _ = stream.read(&mut buf).await.unwrap();
        let packet = Packet::deserialize(&buf);
        debug!("Packet: {:?}", packet);

        assert_eq!(packet.user_id, "test");
    }

    #[tokio::test]
    async fn test_server_100() {
        init();
        tokio::spawn(run_server(44447));

        let mut stream = TcpStream::connect("127.0.0.1:44447").await.unwrap();

        for _ in 0..100 {
            let packet = Packet {
                user_id: String::from("test"),
                time_stamp: String::from("2021-01-01 00:00:00"),
                message: String::from("Hello, world!"),
            };

            let packet_buf = packet.serialize();
            let packet_size = (packet_buf.len() as i32).to_be_bytes();

            let mut buf = BytesMut::new();
            buf.extend_from_slice(&packet_size);
            buf.extend_from_slice(&packet_buf);

            stream.write_all(&buf).await.unwrap();

            let mut buf = [0; 4];
            _ = stream.read(&mut buf).await.unwrap();
            let packet_size = i32::from_be_bytes([buf[0], buf[1], buf[2], buf[3]]) as usize;
            debug!("Packet size: {}", packet_size);

            let mut buf = vec![0; packet_size];
            _ = stream.read(&mut buf).await.unwrap();
            let packet = Packet::deserialize(&buf);
            debug!("Packet: {:?}", packet);

            assert_eq!(packet.user_id, "test");
        }
    }

    #[tokio::test]
    async fn test_server_multiple() {
        init();
        tokio::spawn(run_server(44446));

        const CLIENT_COUNT: usize = 100;
        const PACKET_COUNT: usize = 1000;

        let mut handles = Vec::new();

        for i in 0..CLIENT_COUNT
        {
            let handle = tokio::spawn(async move {
                let mut stream = TcpStream::connect("127.0.0.1:44446").await.unwrap();

                let mut packet_num = i * PACKET_COUNT;

                for _ in 0..PACKET_COUNT
                {
                    let packet = Packet {
                        user_id: String::from("test"),
                        time_stamp: String::from("2021-01-01 00:00:00"),
                        message: format!("Hello, world! {}", packet_num),
                    };

                    let packet_buf = packet.serialize();
                    let packet_size = (packet_buf.len() as i32).to_be_bytes();

                    let mut buf = BytesMut::new();
                    buf.extend_from_slice(&packet_size);
                    buf.extend_from_slice(&packet_buf);

                    stream.write_all(&buf).await.unwrap();

                    packet_num += 1;
                }

                packet_num = i * PACKET_COUNT;

                loop {
                    let mut buf = [0; 4];
                    _ = stream.read(&mut buf).await.unwrap();
                    let packet_size = i32::from_be_bytes([buf[0], buf[1], buf[2], buf[3]]) as usize;

                    let mut buf = vec![0; packet_size];
                    _ = stream.read(&mut buf).await.unwrap();
                    let packet = Packet::deserialize(&buf);

                    if packet.message != format!("Hello, world! {}", packet_num) {
                        continue;
                    }

                    packet_num += 1;

                    if packet_num == (i + 1) * PACKET_COUNT {
                        break;
                    }
                }

                info!("Client {} finished", i);
            });

            handles.push(handle);
        }

        for handle in handles {
            handle.await.unwrap();
        }
    }
}