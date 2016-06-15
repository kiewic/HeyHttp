using HeyHttp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyUnitTests
{
    [TestClass]
    public class HeyHttpAuthenticationUnitTests
    {
        [TestMethod]
        public void GetScheme_Basic()
        {
            Assert.AreEqual(
                AuthorizationScheme.Basic,
                HeyHttpAuthentication.GetScheme("Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ=="));
        }

        [TestMethod]
        public void GetScheme_Digest()
        {
            Assert.AreEqual(
                AuthorizationScheme.Digest,
                HeyHttpAuthentication.GetScheme("Digest username=\"Mufasa\", realm=\"testrealm@host.com\", nonce=\"dcd98b7102dd2f0e8b11d0f600bfb0c093\", uri=\"/dir/index.html\", qop=auth, nc=00000001, cnonce=\"0a4f113b\", response=\"6629fae49393a05397450978507c4ef1\", opaque=\"5ccc069c403ebaf9f0171e9517f40e41\""));
        }

        [TestMethod]
        public void GetScheme_Other()
        {
            Assert.AreEqual(
                AuthorizationScheme.Other,
                HeyHttpAuthentication.GetScheme("Bearer 0b79bab50daca910b000d4f1a2b675d604257e42"));
        }

        public void IsAuthorizationValid_BasicAuthorization_NoHeaders()
        {
            IsAuthorizationValid(
                "/?basic=1",
                "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==",
                false);
        }

        [TestMethod]
        public void IsAuthorizationValid_BasicAuthorization_CorrectUserAndPassword()
        {
            IsAuthorizationValid(
                "/?basic=1&user=Aladdin&password=open%20sesame",
                "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==",
                true);
        }

        [TestMethod]
        public void IsAuthorizationValid_BasicAuthorization_WrongPassword()
        {
            IsAuthorizationValid(
                "/?basic=1&user=Aladdin&password=foo",
                "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==",
                false);
        }

        [TestMethod]
        public void IsAuthorizationValid_BasicAuthorization_WrongUser()
        {
            IsAuthorizationValid(
                "/?basic=1&user=foo&password=open%20sesame",
                "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==",
                false);
        }

        [TestMethod]
        public void IsAuthorizationValid_BasicAuthorization_WrongUserAndPassword()
        {
            IsAuthorizationValid(
                "/?basic=1&user=foo&password=bar",
                "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==",
                false);
        }

        [TestMethod]
        public void IsAuthorizationValid_DigestAuthorization_NoHeaders()
        {
            IsAuthorizationValid(
                "/",
                "Digest QWxhZGRpbjpvcGVuIHNlc2FtZQ==",
                true);
        }

        private void IsAuthorizationValid(string path, string authorizationHeader, bool isValid)
        {
            HeyLogger logger = new HeyLogger();
            HeyHttpRequest request = new HeyHttpRequest(logger);
            request.ParsePath(path);
            request.Authorization = authorizationHeader;

            Assert.AreEqual(isValid, HeyHttpAuthentication.IsAuthorizationValid(request));
        }

        [TestMethod]
        public void AddAuthenticateHeaderIfNeeded_Basic_NoAuthorization()
        {
            HeyLogger logger = new HeyLogger();
            HeyHttpRequest request = new HeyHttpRequest(logger);
            HeyHttpResponse response = new HeyHttpResponse(logger);
            request.ParsePath("/?basic=1");

            HeyHttpAuthentication.AddAuthenticateHeaderIfNeeded(request, response);

            Assert.AreEqual("401 Unauthorized", response.Status);
            Assert.IsTrue(response.Headers.Contains("WWW-Authenticate: Basic realm=\"Secure Area\""));
        }


        [TestMethod]
        public void AddAuthenticateHeaderIfNeeded_Basic_ValidAuthorization()
        {
            HeyLogger logger = new HeyLogger();
            HeyHttpRequest request = new HeyHttpRequest(logger);
            HeyHttpResponse response = new HeyHttpResponse(logger);
            request.ParsePath("/?basic=1&user=foo&password=bar");
            request.Authorization = "Basic Zm9vOmJhcg==";

            HeyHttpAuthentication.AddAuthenticateHeaderIfNeeded(request, response);

            Assert.IsTrue(String.IsNullOrEmpty(response.Status));
            Assert.AreEqual(0, response.Headers.Count);
        }

        [TestMethod]
        public void AddAuthenticateHeaderIfNeeded_Basic_InvalidAuthorization()
        {
            HeyLogger logger = new HeyLogger();
            HeyHttpRequest request = new HeyHttpRequest(logger);
            HeyHttpResponse response = new HeyHttpResponse(logger);
            request.ParsePath("/?basic=1&user=foo&password=bar");
            request.Authorization = "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==";

            HeyHttpAuthentication.AddAuthenticateHeaderIfNeeded(request, response);

            Assert.AreEqual("401 Unauthorized", response.Status);
            Assert.IsTrue(response.Headers.Contains("WWW-Authenticate: Basic realm=\"Secure Area\""));
        }
    }
}
