// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CoreWCF.Http.Tests
{
    public class HttpResponseMessagePropertyTests
    {
        private readonly ITestOutputHelper _output;

        public HttpResponseMessagePropertyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GetCorrespondingConnectionResponseHeaderTest()
        {
            string testString = new string('a', 3000);
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>(_output).Build();

            using (host)
            {
                host.Start();
                System.ServiceModel.ChannelFactory<ClientContract.IEchoService> factory = null;
                ClientContract.IEchoService channel = null;
                try
                {
                    System.ServiceModel.BasicHttpBinding httpBinding = ClientHelper.GetBufferedModeBinding();
                    factory = new System.ServiceModel.ChannelFactory<ClientContract.IEchoService>(
                        httpBinding,
                        new System.ServiceModel.EndpointAddress(
                            new Uri("http://localhost:8080/BasicWcfService/basichttp.svc")));

                    channel = factory.CreateChannel();
                    using (new System.ServiceModel.OperationContextScope(channel as System.ServiceModel.IContextChannel))
                    {
                        var httpRequestMessageProperty = new System.ServiceModel.Channels.HttpRequestMessageProperty();
                        httpRequestMessageProperty.Headers[HttpRequestHeader.Connection] = "close";
                        System.ServiceModel.OperationContext.Current.OutgoingMessageProperties.Add(
                            System.ServiceModel.Channels.HttpRequestMessageProperty.Name,
                            httpRequestMessageProperty);

                        string result = channel.EchoString(testString);

                        var final = System.ServiceModel.OperationContext.Current.IncomingMessageProperties;

                        Assert.Equal(testString, result);
                    }
                }
                finally
                {
                    ServiceHelper.CloseServiceModelObjects(channel as System.ServiceModel.Channels.IChannel, factory);
                }
            }                
        }

        internal class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddServiceModelServices();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseServiceModel(builder =>
                {
                    builder.AddService<Services.EchoService>();
                    builder.AddServiceEndpoint<Services.EchoService, ServiceContract.IEchoService>(
                        new BasicHttpBinding(),
                        "/BasicWcfService/basichttp.svc");
                });
            }
        }
    }
}
