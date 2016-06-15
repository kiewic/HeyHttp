using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HeyHttp.Core;

namespace HeyUnitTests
{
    [TestClass]
    public class ArgsParserUnitTests
    {
        [TestMethod]
        public void NoArguments()
        {
            string[] args = new string[] { "https", "server" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(false, argsParser.HasOption("ClientCertificate"));

            string issuerName;
            Assert.AreEqual(false, argsParser.TryGetOptionValue("IssuerName", out issuerName));

            string subjectName;
            Assert.AreEqual(false, argsParser.TryGetOptionValue("SubjectName", out subjectName));
        }

        [TestMethod]
        public void UnamedArgument()
        {
            string[] args = new string[] { "http", "client", "http://example.com" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(false, argsParser.HasOption("Head"));

            string uriString = String.Empty;
            Assert.AreEqual(true, argsParser.TryGetString(2, ref uriString));
            Assert.AreEqual("http://example.com", uriString);
        }

        [TestMethod]
        public void OptionArgument()
        {
            string[] args = new string[] { "https", "server", "-ClientCertificate" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(true, argsParser.HasOption("ClientCertificate"));

            string issuerName;
            Assert.AreEqual(false, argsParser.TryGetOptionValue("IssuerName", out issuerName));

            string subjectName;
            Assert.AreEqual(false, argsParser.TryGetOptionValue("SubjectName", out subjectName));
        }

        [TestMethod]
        public void OptionArgumentCaseInsensitive()
        {
            string[] args = new string[] { "https", "server", "-ClientCertificate" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(true, argsParser.HasOption("clientcertificate"));
        }

        [TestMethod]
        public void OptionArgumentAndUnamedArgument()
        {
            string[] args = new string[] { "http", "client", "-Head", "http://example.com" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(true, argsParser.HasOption("Head"));

            string uriString = String.Empty;
            Assert.AreEqual(true, argsParser.TryGetString(2, ref uriString));
            Assert.AreEqual("http://example.com", uriString);
        }

        [TestMethod]
        public void OptionArgumentAndTwoOptionArgumentsWithStringValue()
        {
            string[] args = new string[] { "https", "server", "-ClientCertificate", "-SubjectName", "CN=http2.cloudapp.net", "-IssuerName", "CN=example.com" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(true, argsParser.HasOption("ClientCertificate"));

            string issuerName;
            Assert.AreEqual(true, argsParser.TryGetOptionValue("IssuerName", out issuerName));
            Assert.AreEqual("CN=example.com", issuerName);

            string subjectName;
            Assert.AreEqual(true, argsParser.TryGetOptionValue("SubjectName", out subjectName));
            Assert.AreEqual("CN=http2.cloudapp.net", subjectName);
        }

        [TestMethod]
        public void TwoOptionArgumentsWithStringValue()
        {
            string[] args = new string[] { "https", "server", "-SubjectName", "CN=http2.cloudapp.net", "-IssuerName", "CN=example.com" };
            ArgsParser argsParser = new ArgsParser(args);

            Assert.AreEqual(false, argsParser.HasOption("ClientCertificate"));

            string issuerName;
            Assert.AreEqual(true, argsParser.TryGetOptionValue("IssuerName", out issuerName));
            Assert.AreEqual("CN=example.com", issuerName);

            string subjectName;
            Assert.AreEqual(true, argsParser.TryGetOptionValue("SubjectName", out subjectName));
            Assert.AreEqual("CN=http2.cloudapp.net", subjectName);
        }

        [TestMethod]
        public void OptionArgumentWithIntValueAndOptionArgumentWithStringValue()
        {
            string[] args = new string[] { "proxy", "server", "-Port", "8080", "-Src", "192.168.0.0-192.168.255.255" };
            ArgsParser argsParser = new ArgsParser(args);

            int port;
            Assert.AreEqual(true, argsParser.TryGetOptionValue("Port", out port));
            Assert.AreEqual(8080, port);

            string src; ;
            Assert.AreEqual(true, argsParser.TryGetOptionValue("Src", out src));
            Assert.AreEqual("192.168.0.0-192.168.255.255", src);

            // Trying to get something else should be false.
            string somethingElse = String.Empty;
            Assert.AreEqual(false, argsParser.TryGetString(2, ref somethingElse));
        }

        [TestMethod]
        public void ThreeUnamedArguments()
        {
            string[] args = new string[] { "udp", "sender", "example.com", "8080", "Hello World!" };
            ArgsParser argsParser = new ArgsParser(args);

            string hostname = String.Empty;
            Assert.AreEqual(true, argsParser.TryGetString(2, ref hostname));
            Assert.AreEqual("example.com", hostname);

            int port = 0;
            argsParser.TryGetInt32(3, ref port);
            Assert.AreEqual(8080, port);

            string message = String.Empty;
            argsParser.TryGetString(4, ref message);
            Assert.AreEqual("Hello World!", message);
        }
    }
}
