# What are HeyHttp and heyhttp.org?

**HeyHttp** is a .NET C# console application that you can use to test your HTTP clients. It consists of: 

* A very light HTTP server.
* A simple HTTPS server.
* Other networking protocols, such as HTTP CONNECT, WebSocket, and UDP are also included.

**heyhttp.org** is a public server running multiple instances of **HeyHttp**, each with different configurations, for example:

* `http://heyhttp.org/` is a plain HTTP endpoint.
* `https://heyhttp.org/` is an HTTPS endpoint.
* `https://heyhttp.org:8080/` is an HTTPS endpoint that requires a client certificate.

> **CAUTION!**  
> All the servers and clients included in this project are partially implemented.

## HTTP Server Features

Some features you can try are:

* `http://heyhttp.org/?delay=5000` to introduce a 5 seconds delay before starting to send a response.
* `http://heyhttp.org/?pause=1` to pause the writing of a response until the ENTER key is pressed.
* `http://heyhttp.org/?slow=1000` to receive a response slowly, e.g., this configuration sends a package every 1000 milliseconds.
* `http://heyhttp.org/?length=10240` to receive a response of 10240 bytes.
* `http://heyhttp.org/?bufferLength=1024` to receive a response in chunks of 1024 bytes.
* `http://heyhttp.org/?idleLength=2048` to indefinitely block the response thread until the client is disconnected when `bufferLength * N >= idleLength`.

You can mix options, for example:

    http://heyhttp.org/?slow=1000&bufferLength=1000&length=1000000

More options you can also mix:

* `http://heyhttp.org/?cache=1` to include headers in the response so content can be cached.
* `http://heyhttp.org/?nocache=1` to include headers in the response that will prevent the content from being cached.
* `http://heyhttp.org/?chunked=1` to receive a response with chunked transfer coding, i.e., using `Transfer-Encoding: chunked`
* `http://heyhttp.org/?gzip=1` to receive a response with GZIP coding, i.e., `Transfer-Encoding: gzip`.
* `http://heyhttp.org/?setcookie=1` to receive a response with multiple `Set-Cookie` headers.
* `http://heyhttp.org/?etag=1234` to receive a response with an `ETag: 1234` header and an `Accept-Ranges` header.
* `http://heyhttp.org/?lastModified=1` to receive a response with a `Last-Modified` header and an `Accept-Ranges` header.
* `http://heyhttp.org/?filename=something.ext` to receive a response with a `Content-Disposition: attachment; filename=something.ext` header, so a client knows the response can be stored in the file system using the suggested name.
* `http://heyhttp.org/?status=400` to receive a response with a `400 Not Found` status.
* `http://heyhttp.org/?redirect=http%3A%2F%2Fexample.com` to receive a `301 Moved Permanently` response and a `Location: http://example.com` header.
* `http://heyhttp.org/?retry=1` to receive a `503 Service Unavailable` response with a `Retry-After: 5` header.
* `http://heyhttp.org/?basic=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate Basic` header.
* `http://heyhttp.org/?digest=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate Digest` header.
* `http://heyhttp.org/?negotiate=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate Negotiate` header.
* `http://heyhttp.org/?ntlm=1` to receive a `401 Unauthorized` response with a `WWW-Authenticate NTLM` header.
* `http://heyhttp.org/?user=foo` to set the expected user name. If the request user name does not match *foo*, the response status is `401 Unauthorized`.
* `http://heyhttp.org/?password=bar` to set the expected password. If the request password does not match *bar*, the response status is `401 Unauthorized`.

It is common to use user and password options at the same time, for example:

    http://heyhttp.org/?basic=1&user=foo&password=bar

Some more possible options:

* `http://heyhttp.org/?name=Foo&value=Bar` to receive a response with a `Foo: Bar` header.
* `http://heyhttp.org/?custom=1` to add three random custom headers.
* `http://heyhttp.org/?trace=1` to receive the request headers back, as the content of the response.


## HeyHttp.exe Features

To start the HTTP server, run HeyHttp.exe in the command prompt as:

    HeyHttp.exe http server

To start the HTTPS server run the command:

    HeyHttp.exe https server

To ask for a client certificate when a response is received run the command:

    HeyHttp.exe https server -ClientCertificate

To select a server certificate run the command (for example):

    HeyHttp.exe https server -Thumbprint "07261b17e0d71247b185234335c6126bc2796b6b"

More features:

* Press `CTRL + C` to kill all the current connections.
* Press `CTRL + BREAK` to kill the process.
* HeyHttp.exe supports `Keep-Alive` connections.
* HeyHttp.exe supports `Range` and `Content-Range` request headers.
* It serves files from the file system, e.g., `http://heyhttp.org/bar/foo.txt` returns the file at `.\bin\Debug\wwwroot\bar\foo.txt`.
* Do you want transport layer logs? Use LogLevel.TransportLayer. Application level logs? Use LogLevel.ApplicationLayer.
* HeyHttp.exe supports dual-mode sockets.



## Why another HTTP server?

1. HeyHttp is written in C#! It is easy for C# developers to modify it and add new features.
2. HeyHttp is written with raw sockets! Developers can introduce failures or delays at any point in the protocol stack, e.g., write a malformed response for diagnostic purposes.


## How to get started?

Well, there are two easy ways to get started.

### Option 1

Download the binary and run it like this:

    HeyHttp.exe http server

Or, if you want to use it as a client, like this:

    HeyHttp.exe http client http://bing.com

Or, if you want to use it as a TCP server, like this:

    HeyHttp.exe tcp server

Or, if you want to read other command line options, like this:

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
