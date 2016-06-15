using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    public enum AuthorizationScheme
    {
        Other,
        Basic,
        Digest
    }

    public enum NtlmMessageType : uint
    {
        NegotiatieMessage = 1,
        ChallengeMessage = 2,
        AuthenticateMessage = 3
    }

    public class HeyHttpAuthentication
    {
        private const string digestRealm = "testrealm@host.com";
        private const string digestQop = "auth"; // "auth,auth-int";
        private const string digestNonce = "dcd98b7102dd2f0e8b11d0f600bfb0c093";
        private const string digestOpaque = "5ccc069c403ebaf9f0171e9517f40e41";

        public static void AddAuthenticateHeaderIfNeeded(HeyHttpRequest request, HeyHttpResponse response)
        {
            // Basic Server:
            // GET --------->
            //
            // <--------- 401 Unauthorized
            //            WWW-Authenticate: Basic realm="xyz"
            //
            // GET --------->
            // Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==
            //
            // <--------- 200 OK

            // Basic Proxy:
            //
            // GET --------->
            // <-------- 407 Proxy Authentication Required
            //           Proxy-Authenticate: Basic realm="xyz"
            //
            // GET --------->
            // Proxy-Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==
            //
            // <--------- 200 OK

            // NTLM:
            //
            // GET --------->
            // <--------- 401 Unauthorized
            // negotiate message --------->
            // <--------- challenge message
            // authenticate message --------->
            // <--------- 200 OK
            if (IsNtlmMessage(request, NtlmMessageType.NegotiatieMessage))
            {
                response.Status = "401 Unauthorized";
                response.Headers.Add("WWW-Authenticate: NTLM TlRMTVNTUAACAAAADgAOADgAAAAFgomiBTEwAGt4s6QAAAAAAAAAAPwA/ABGAAAABgLwIwAAAA9SAEUARABNAE8ATgBEAAIADgBSAEUARABNAE8ATgBEAAEAHgBXAEkATgAtAEQARgBHADEAMABFADIAOABLADEANgAEADQAcgBlAGQAbQBvAG4AZAAuAGMAbwByAHAALgBtAGkAYwByAG8AcwBvAGYAdAAuAGMAbwBtAAMAVABXAEkATgAtAEQARgBHADEAMABFADIAOABLADEANgAuAHIAZQBkAG0AbwBuAGQALgBjAG8AcgBwAC4AbQBpAGMAcgBvAHMAbwBmAHQALgBjAG8AbQAFACQAYwBvAHIAcAAuAG0AaQBjAHIAbwBzAG8AZgB0AC4AYwBvAG0ABwAIADJX/d1Cjc0BAAAAAA==");
            }

            // Require basic access authentication.
            if (request.QueryStringHasTrueValue("basic") && !IsAuthorizationValid(request))
            {
                response.Status = "401 Unauthorized";
                response.Headers.Add("WWW-Authenticate: Basic realm=\"Secure Area\"");
                request.Path = "";
            }

            // Require digest access authentication.
            if (request.QueryStringHasTrueValue("digest") && !IsAuthorizationValid(request))
            {
                response.Status = "401 Unauthorized";
                response.Headers.Add(String.Format(
                    "WWW-Authenticate: Digest realm=\"{0}\", qop=\"{1}\", nonce=\"{2}\", opaque=\"{3}\"",
                    digestRealm,
                    digestQop,
                    digestNonce,
                    digestOpaque));
                request.Path = "";
            }

            // Require NTLM credentials.
            if (request.QueryStringHasTrueValue("negotiate") && !IsAuthorizationValid(request))
            {
                response.Status = "401 Unauthorized";
                response.Headers.Add("WWW-Authenticate: Negotiate");
                request.Path = "";
            }

            // NTLM authentication.
            if (request.QueryStringHasTrueValue("ntlm") && !IsAuthorizationValid(request))
            {
                response.Status = "401 Unauthorized";
                response.Headers.Add("WWW-Authenticate: NTLM");
                request.Path = "";
            }
        }

        public static bool IsAuthorizationValid(HeyHttpRequest request)
        {
            string user = String.Empty;
            string password = String.Empty;

            // 1. Decode headers.

            if (!String.IsNullOrEmpty(request.Authorization))
            {
                switch (GetScheme(request.Authorization))
                {
                    case AuthorizationScheme.Basic:
                        ProcessBasicAuthorization(request.Authorization, out user, out password);
                        break;
                    default:
                        // We cannot decode this header, assume header is valid.
                        return true;
                }
            }

            // 2. Look for expected values and compare.

            string expectedUser;
            string expectedPassword;
            if (request.QueryStringHas("user", out expectedUser) &&
                request.QueryStringHas("password", out expectedPassword))
            {
                if (expectedUser == user && expectedPassword == password)
                {
                    return true;
                }
            }

            return false;
        }

        public static AuthorizationScheme GetScheme(string headerValue)
        {
            if (String.IsNullOrEmpty(headerValue))
            {
                return AuthorizationScheme.Other;
            }

            if (headerValue.StartsWith("Basic "))
            {
                return AuthorizationScheme.Basic;
            }

            if (headerValue.StartsWith("Digest "))
            {
                return AuthorizationScheme.Digest;
            }

            return AuthorizationScheme.Other;
        }

        private static void ProcessBasicAuthorization(string headerValue, out string user, out string password)
        {
            string usernameAndPasswordEncoded = headerValue.Substring("Basic ".Length);
            byte[] bytes = Convert.FromBase64String(usernameAndPasswordEncoded);
            string usernameAndPassword = Encoding.UTF8.GetString(bytes);

            if (!usernameAndPassword.Contains(":"))
            {
                user = usernameAndPassword;
                password = String.Empty;
            }

            String[] pieces = usernameAndPassword.Split(new char[] { ':' }, 2);
            user = pieces[0];
            password = pieces[1];
        }

        private static void ProcessDigestAuthorization(HeyHttpRequest request, HeyLogger logger)
        {
            string headerValue = request.Authorization.Substring("Digest ".Length);
            string[] pairs = headerValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] keyAndValue = pair.Split(new char[] {'='}, 2);
                if (keyAndValue.Length == 2)
                {
                    string key = keyAndValue[0].Trim();
                    string value = keyAndValue[1].Trim('"');

                    logger.WriteHeadersLine(
                        String.Format("{0}: {1}", key, value));
                }
            }

            logger.WriteHeadersLine("\r\n");

            // TODO: If the password is not the expected one, clear Authorization header,
            // so a 401 Unauthorized response is sent.
        }

        #region NTLM Authentication

        private static bool IsNtlmAuthentication(HeyHttpRequest request)
        {
            if (String.IsNullOrEmpty(request.Authorization) || !request.Authorization.StartsWith("NTLM "))
            {
                return false;
            }
            return true;
        }

        private static bool IsNtlmMessage(HeyHttpRequest request, NtlmMessageType expectedMessageType)
        {
            if (!IsNtlmAuthentication(request))
            {
                return false;
            }

            string message = request.Authorization.Substring("NTLM ".Length);
            byte[] buffer = Convert.FromBase64String(message);

            // Validate signature.
            string signature = Encoding.ASCII.GetString(buffer, 0, 8);
            if (signature != "NTLMSSP\0")
            {
                return false;
            }

            // Is it negotiation message?
            uint messageType = BitConverter.ToUInt32(buffer, 8);
            if (messageType != (uint)expectedMessageType)
            {
                return false;
            }

            if (expectedMessageType == NtlmMessageType.NegotiatieMessage)
            {
                // Domain name.
                bool hasDomainName = (buffer[14] & 0x10) == 1;
                uint domainNameLength = BitConverter.ToUInt16(buffer, 16);
                uint domainNameMaxLength = BitConverter.ToUInt16(buffer, 18);
                uint domainNameOffset = BitConverter.ToUInt32(buffer, 20);
            }

            return true;
        }

        #endregion NTLM Authentication

    }
}
