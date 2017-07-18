taskkill /f /im HeyHttp.exe

set HeyFullPath=%~dp0HeyHttp.exe
netsh advfirewall firewall add rule name="Allow HeyHttp" dir=in action=allow program="%HeyFullPath%"

start %~dp0HeyHttp.exe http server
start %~dp0HeyHttp.exe https server -Thumbprint "c0f75f37a51dad71181f682ccf7eed41d8872703"
start %~dp0HeyHttp.exe https server 8080 -Thumbprint "c0f75f37a51dad71181f682ccf7eed41d8872703" -ClientCertificate

rem Expired certificate
start %~dp0HeyHttp.exe https server 8081 -Thumbprint "07261b17e0d71247b185234335c6126bc2796b6b"
