﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kafka.Client.Util;

namespace Kafka.Client
{
    /// <summary>
    /// Consumes messages from Kafka.
    /// </summary>
    public class Consumer
    {
        /// <summary>
        /// Maximum size.
        /// </summary>
        private static readonly int MaxSize = 1048576;

        /// <summary>
        /// Initializes a new instance of the Consumer class.
        /// </summary>
        /// <param name="server">The server to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public Consumer(string server, int port)
        {
            Server = server;
            Port = port;
        }

        /// <summary>
        /// Gets the server to which the connection is to be established.
        /// </summary>
        public string Server { get; private set; }

        /// <summary>
        /// Gets the port to which the connection is to be established.
        /// </summary>
        public int Port { get; private set; }

        public List<Message> Consume(string topic, int partition, long offset)
        {
            return Consume(topic, partition, offset, MaxSize);
        }

        public  List<Message> Consume(string topic, int partition, long offset, int maxSize)
        {
            // REQUEST TYPE ID + TOPIC LENGTH + TOPIC + PARTITION + OFFSET + MAX SIZE
            int requestSize = 2 + 2 + topic.Length + 4 + 8 + 4;

            List<byte> request = new List<byte>();
            request.AddRange(BitWorks.GetBytesReversed(requestSize));
            request.AddRange(BitWorks.GetBytesReversed((short)RequestType.Fetch));
            request.AddRange(BitWorks.GetBytesReversed((short)topic.Length));
            request.AddRange(Encoding.ASCII.GetBytes(topic));
            request.AddRange(BitWorks.GetBytesReversed(partition));
            request.AddRange(BitWorks.GetBytesReversed(offset));
            request.AddRange(BitWorks.GetBytesReversed(maxSize));

            List<Message> messages = new List<Message>();
            using (KafkaConnection connection = new KafkaConnection(Server, Port))
            {
                connection.Write(request.ToArray<byte>());
                int dataLength = BitConverter.ToInt32(BitWorks.ReverseBytes(connection.Read(4)), 0);

                if (dataLength > 0) 
                {
                    byte[] data = connection.Read(dataLength);

                    // remove two byte buffer
                    byte[] unbufferedData = data.Skip(2).ToArray();

                    int processed = 0;
                    int length = unbufferedData.Length - 4;
                    int messageSize = 0;
                    while(processed <= length) 
                    {
                        messageSize = BitConverter.ToInt32(BitWorks.ReverseBytes(unbufferedData.Skip(processed).Take(4).ToArray<byte>()), 0);
                        messages.Add(Message.ParseFrom(unbufferedData.Skip(processed).Take(messageSize + 4).ToArray<byte>()));
                        processed += 4 + messageSize;
                    }
                }
            }

            return messages;
        }
    }
}
