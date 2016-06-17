# What are HeyHttp and heyhttp.org?

**HeyHttp** is a .NET C# console application that contains: 

* The lightest HTTP server.
* The simplest HTTPS server.
* Other networking protocols are also included.

**heyhttp.org** is a public server running multiple instances of **HeyHttp**, each with different configurations, e.g.:

* `http://heyhttp.org/` is a plain HTTP endpoint.
* `https://heyhttp.org/` is an HTTPS endpoint.
* `https://heyhttp.org:8080/` is an HTTPS endpoint that requires a client certificate.

> **CAUTION!**  
> All the servers and clients included in this project are partially implemented.

## HTTP Server Features

Some features you can try are:

* `http://heyhttp.org/?delay=5000` to introduce a 5 seconds delay before start sending the response.
* `http://heyhttp.org/?pause=1` to pause the writing of the response until the ENTER key is pressed.
* `http://heyhttp.org/?slow=1000` to receive the response slowly. This configuration sends a package every 1000 milliseconds.
* `http://heyhttp.org/?length=10240` to receive a 10240 bytes response.
* `http://heyhttp.org/?bufferLength=1024` to receive the response in chunks of 1024 bytes.
* `http://heyhttp.org/?idleLength=2048` to block the response thread until client is disconnected when 'bufferLength * N >= idleLength'.

You can mix options, e.g.:

    http://heyhttp.org/?slow=1000&bufferLength=1000&length=1000000

* `http://heyhttp.org/?cache=1` to include headers in the response so content can be cached.
* `http://heyhttp.org/?nocache=1` to include headers in the response so content cannot be cached.
* `http://heyhttp.org/?chunked=1` to receive a chunked encoded response.
* `http://heyhttp.org/?gzip=1` to receive a GZIP encoded response.
* `http://heyhttp.org/?setcookie=1` to receive a response with multiple `Set-Cookie` headers.
* `http://heyhttp.org/?etag=1234` too receive a response with an `ETag: 1234` header and an `Accept-Ranges` header.
* `http://heyhttp.org/?lastModified=1` too receive a response with a `Last-Modified` header and an `Accept-Ranges` header.
* `http://heyhttp.org/?filename=something.txt` to receive a response with a `Content-Disposition: attachment; filename=something.txt`.
* `http://heyhttp.org/?status=400` to receive a response with a 400 status.
* `http://heyhttp.org/?redirect=http%3A%2F%2Fexample.com` to receive a `301 Moved Permanently` response and a `Location: http://example.com` header.
* `http://heyhttp.org/?retry=1` to receive a `503 Service Unavailable` response with a `Retry-After: 5` header.
* `http://heyhttp.org/?basic=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate Basic` header.
* `http://heyhttp.org/?digest=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate Digest` header.
* `http://heyhttp.org/?negotiate=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate Negotiate` header.
* `http://heyhttp.org/?ntlm=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate NTLM` header.
* `http://heyhttp.org/?user=foo` to set the expected user name, if user name does not match response status is `401 Unauthorized`.
* `http://heyhttp.org/?password=bar` to set the expected password, if password does not match response status is `401 Unauthorized`.

It is common to use user and password options at the same time, e.g.:

    http://heyhttp.org/?basic=1&user=foo&password=bar

* `http://heyhttp.org/?name=Foo&value=Bar` to receive a response with a `Foo: Bar` header.
* `http://heyhttp.org/?custom=1` to add three unknown custom headers.
* `http://heyhttp.org/?trace=1` to receive the request headers back (as the content of the response).


## HeyHttp.exe Features

To start the HTTP server, run HeyHttp.exe as:

    HeyHttp.exe http server

To start the HTTPS server run as:

    HeyHttp.exe https server

To ask for a client certificate when a response is received run as:

    HeyHttp.exe https server -ClientCertificate

To select a server certificate run as:

    HeyHttp.exe https server -Thumbprint "07261b17e0d71247b185234335c6126bc2796b6b"

Features:

* Press `CTRL + C` to kill all the current connections.
* Press `CTRL + BREAK` to kill the process.
* Supports `Keep-Alive` connections.
* Supports `Range` and `Content-Range` headers.
* It serves files from the file system, e.g., `http://heyhttp.org/bar/foo.txt` returns the file at `.\bin\Debug\wwwroot\bar\foo.txt`.
* Do you want transport layer logs? Use LogLevel.TransportLayer. Application level logs? Use LogLevel.ApplicationLayer.
* Dual-mode sockets supported.



## Why another HTTP server?

1. HeyHttp is written in C#! It is easy for C# developers to modify it and add new features.
2. HeyHttp is written with raw sockets! Developers can introduce a failure or delay at any point in the protocol stack, e.g., malformed responses.


## How to get started?

Well, there are two easy ways to get started.

### Option 1

Download the binary and run it like this:

    HeyHttp.exe http server

Or like this:

    HeyHttp.exe http client http://bing.com

Or like this:

    HeyHttp.exe tcp server

Or read other command line options:

    HeyHttp.exe /?

### Option 2

1. Create a Git enlistment: 
   ```
   git clone https://github.com/kiewic/heyhttp
   ```
2. Open the HeyHttp.sln solution.
3. Build (Ctrl + Shift + B)
4. Start Debugging (F5)


## Mission

The HeyHttp mission is to provide servers and clients for testing, capable of reproduce scenarios that hardly occur in normal conditions using conventional servers and clients.

Everything is written with raw sockets, so it is possible to modify the protocols at any step.


## Other networking protocols included in HeyHttp

* WebSocket server.
* TCP server.
* TCP client.
* UDP receiver.
* UDP sender.
* SSL server.
* SSL client.
* HTTP proxy. 
* HTTP CONNECT tunnel.


## How does the HTTP client work?

Have you ever wonder why a website is or it is not caching? Is a website being redirected? Is a website setting cookies? This HTTP client is the easiest way to find out the answers.

To start the HTTP client type:

    HeyHttp.exe http client http://my.msn.com

Add an extra header:

    http client http://heyhttp.org/?ntlm=1 -H "Authorization: NTLM TlRMTVNTUAABAAAAA7IAAAoACgApAAAACQAJACAAAABMSUdIVENJVFlVUlNBLU1JTk9S"

Use a different HTTP method:

    http client http://heyhttp.org/?ntlm=1 -X HEAD

## How do the UDP sender and UDP receiver work?

To start the UDP receiver type:

    HeyHttp.exe udp receiver [port]

To start the UDP sender type:

    HeyHttp.exe udp sender [hostname] [port] [message]

On receiver mode, it listens for UDP packets in the given port.

On sender mode, it sends a package with the given message to the given hostname and port.



## How does the SSL server work?

To start the SSL server type:

    HeyHttp.exe ssl server

To configure the server certificate, please read the instructions in the Certificates.md file.


## How does the HTTP proxy server work?

To start the HTTP proxy server type:

    HeyHttp.exe proxy server

To skip proxy authentication type:

    HeyHttp.exe proxy server -NoAuthentication

To configure a proxy:

1. Open "Internet Properties", i.e., inetcpl.cpl
2. Click "Connections" tab.
3. Click "LAN settings" button.
4. Check "Use a proxy server for your LAN ...".
5. Type "localhost" in the "Address" field.
6. Type "3333" (or whatever port number you are planning to use) in the "Port" field.
7. Click "Ok" button twice.

Features:

* Supports SSL connections through HTTP CONNECT Tunneling.
