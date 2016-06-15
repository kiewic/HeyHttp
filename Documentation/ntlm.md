NTLM Example
============

1. The client requests a protected resource from the server:

        GET /index.html HTTP/1.1

2. The server responds with a 401 status, indicating that the client must authenticate. "NTLM" is presented as a supported authentication mechanism via the "WWW-Authenticate" header. Typically, the server closes the connection at this time:

        HTTP/1.1 401 Unauthorized
        WWW-Authenticate: NTLM
        Connection: close

    Note that Internet Explorer will only select NTLM if it is the first mechanism offered; this is at odds with RFC 2616, which states that the client must select the strongest supported authentication scheme.

3. The client resubmits the request with an "Authorization" header containing a Type 1 message parameter. The Type 1 message is Base-64 encoded for transmission. From this point forward, the connection is kept open; closing the connection requires reauthentication of subsequent requests. This implies that the server and client must support persistent connections, via either the HTTP 1.0-style "Keep-Alive" header or HTTP 1.1 (in which persistent connections are employed by default). The relevant request headers appear as follows (the line break in the "Authorization" header below is for display purposes only, and is not present in the actual message):

        GET /index.html HTTP/1.1
        Authorization: NTLM TlRMTVNTUAABAAAABzIAAAYABgArAAAACwALACAAAABXT1
        JLU1RBVElPTkRPTUFJTg==

4. The server replies with a 401 status containing a Type 2 message in the "WWW-Authenticate" header (again, Base-64 encoded). This is shown below (the line breaks in the "WWW-Authenticate" header are for editorial clarity only, and are not present in the actual header).

        HTTP/1.1 401 Unauthorized
        WWW-Authenticate: NTLM TlRMTVNTUAACAAAADAAMADAAAAABAoEAASNFZ4mrze8
        AAAAAAAAAAGIAYgA8AAAARABPAE0AQQBJAE4AAgAMAEQATwBNAEEASQBOAAEADABTA
        EUAUgBWAEUAUgAEABQAZABvAG0AYQBpAG4ALgBjAG8AbQADACIAcwBlAHIAdgBlAHI
        ALgBkAG8AbQBhAGkAbgAuAGMAbwBtAAAAAAA=

5. The client responds to the Type 2 message by resubmitting the request with an "Authorization" header containing a Base-64 encoded Type 3 message (again, the line breaks in the "Authorization" header below are for display purposes only):

        GET /index.html HTTP/1.1
        Authorization: NTLM TlRMTVNTUAADAAAAGAAYAGoAAAAYABgAggAAAAwADABAAA
        AACAAIAEwAAAAWABYAVAAAAAAAAACaAAAAAQIAAEQATwBNAEEASQBOAHUAcwBlAHIA
        VwBPAFIASwBTAFQAQQBUAEkATwBOAMM3zVy9RPyXgqZnr21CfG3mfCDC0+d8ViWpjB
        wx6BhHRmspst9GgPOZWPuMITqcxg==

6. Finally, the server validates the responses in the client's Type 3 message and allows access to the resource.
    HTTP/1.1 200 OK

Source: http://davenport.sourceforge.net/ntlm.html
