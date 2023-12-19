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
        buf.extend_from_slice(self.time_stamp.as_bytes());
        buf.extend_from_slice(self.message.as_bytes());
        buf
    }

    fn deserialize(buf: &[u8]) -> Self {
        let user_id = String::from_utf8_lossy(&buf[0..10]).to_string();
        let time_stamp = String::from_utf8_lossy(&buf[10..20]).to_string();
        let message = String::from_utf8_lossy(&buf[20..]).to_string();
        Self {
            user_id,
            time_stamp,
            message,
        }
    }
}

#[tokio::main]
async fn main() {
    let tcp = TcpListener::bind("127.0.0.1:7777").await.unwrap();

    let mut writers: Vec<OwnedWriteHalf> = Vec::new();

    let (sender, receiver) = unbounded::<Packet>();
    let (socket_sender, socket_receiver) = unbounded::<TcpStream>();

    tokio::spawn(async move {
        loop {
            let (socket, _) = tcp.accept().await.unwrap();
            socket_sender.send(socket).unwrap();
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
                    let n = reader.read(&mut buf).await.unwrap();
                    if n == 0 {
                        return;
                    }
                    let packet = Packet::deserialize(&buf);
                    sender.send(packet).unwrap();
                }
            });
        }

        if let Ok(packet) = receiver.try_recv() {
            for writer in writers.iter_mut() {
                writer.write_all(&packet.serialize()).await.unwrap();
            }
        }
    }
}
