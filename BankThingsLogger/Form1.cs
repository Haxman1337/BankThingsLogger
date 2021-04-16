using System;
using Microsoft.Web.Services2.Security.Tokens;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BankThingsLogger.ServiceReference1;
using System.Xml;

namespace BankThingsLogger
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }



        private void goBtn_Click(object sender, EventArgs e)
        {
            try
            {
                //System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                orderStatusResponse sberOrder = null;
                string address = adrTb.Text;
                string login = logTb.Text;
                string password = passTb.Text;
                string id = idTb.Text;
                using (var context = new MerchantServiceClient("MerchantServiceImplPort", address))
                {
                    context.Endpoint.EndpointBehaviors.Add(new EndpointAddCredentials(login, password));
                    context.ClientCredentials.UserName.UserName = login;
                    context.ClientCredentials.UserName.Password = password;
                    var orderReq = new orderStatusRequest();
                    orderReq.orderId = id;
                    sberOrder = context.getOrderStatus(orderReq);
                    if (sberOrder.errorCode != 0)
                        outputRtb.Text += $"Ошибка при запросе статуса платежа в Сбербанке: errorCoder - {sberOrder.errorCode}, errorMessage - {sberOrder.errorMessage}" + "\n";
                    var orderInfoReq = new getOrderStatusExtendedRequest();
                    orderInfoReq.orderId = id;
                    getOrderStatusExtendedResponse resp = context.getOrderStatusExtended(orderInfoReq);

                    string reply = connector.replySoap;

                    outputRtb.Text += reply + "\n";
                    outputRtb.Text += "--------------------" + "\n";

                    filenameLbl.Text = "Saved as: " + connector.filename;
                    timer1.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                outputRtb.Text += "--------------------" + "\n";
                outputRtb.Text += ex.Message + "\n";
                outputRtb.Text += "--------------------" + "\n";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            filenameLbl.Text = "";
            timer1.Enabled = false;
        }
    }

    public static class connector
    {
        public static string replySoap = "";
        public static string filename = "";
    }

    public class EndpointAddCredentials : IEndpointBehavior
    {
        private string _username;
        private string _password;

        public EndpointAddCredentials(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new SimpleMessageInspector(_username, _password));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }

    }

    public class SimpleMessageInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        private string _username;
        private string _password;

        public SimpleMessageInspector(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            string filename = System.DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".log";
            connector.replySoap = reply.ToString();
            connector.filename = filename;
            File.WriteAllText(filename, reply.ToString());
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
        {
            UsernameToken authentication = new UsernameToken(_username, _password, PasswordOption.SendPlainText);

            var webUserHeader = MessageHeader.CreateHeader("Security",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", authentication.GetXml(new XmlDocument()));
            request.Headers.Add(webUserHeader);

            return null;
        }

        public object AfterReceiveRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return null;
        }

        public void BeforeSendReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
        }
    }
}
