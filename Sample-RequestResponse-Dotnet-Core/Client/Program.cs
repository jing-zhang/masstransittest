﻿namespace Client
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.Util;
    using Sample.MessageTypes;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.FileExtensions;
    using Microsoft.Extensions.Configuration.Json;
    class Program
    {
        static void Main()
        {
            IBusControl busControl = CreateBus();
            TaskUtil.Await(() => busControl.StartAsync());

            try
            {
                IRequestClient<ISimpleRequest, ISimpleResponse> client = CreateRequestClient(busControl);

                for (; ; )
                {
                    Console.Write("Enter customer id (quit exits): ");
                    string customerId = Console.ReadLine();
                    if (customerId == "quit")
                        break;

                    // this is run as a Task to avoid weird console application issues
                    Task.Run(async () =>
                    {
                        ISimpleResponse response = await client.Request(new SimpleRequest(customerId));

                        Console.WriteLine("Customer Name: {0}", response.CusomerName);
                    }).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception!!! OMG!!! {0}", ex);
            }
            finally
            {
                busControl.Stop();
            }
        }


        static IRequestClient<ISimpleRequest, ISimpleResponse> CreateRequestClient(IBusControl busControl)
        {
            var serviceAddress = new Uri(Configuration["ServiceAddress"]);
            IRequestClient<ISimpleRequest, ISimpleResponse> client =
                busControl.CreateRequestClient<ISimpleRequest, ISimpleResponse>(serviceAddress, TimeSpan.FromSeconds(10));

            return client;
        }

        static IBusControl CreateBus()
        {
            return Bus.Factory.CreateUsingRabbitMq(x => x.Host(new Uri(Configuration["RabbitMQHost"]), h =>
            {
                h.Username("username");
                h.Password("password");
            }));
        }


        private static IConfigurationRoot Configuration
        {
            get
            {
                var config = new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json", true, true)
                       .Build();
                return config  ;
            }
        }
    }
    class SimpleRequest : ISimpleRequest
    {
        readonly string _customerId;
        readonly DateTime _timestamp;

        public SimpleRequest(string customerId)
        {
            _customerId = customerId;
            _timestamp = DateTime.UtcNow;
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        public string CustomerId
        {
            get { return _customerId; }
        }
    }
}