taskkill /f /im HeyHttp.exe

set HeyFullPath=%~dp0HeyHttp.exe
netsh advfirewall firewall add rule name="Allow HeyHttp" dir=in action=allow program="%HeyFullPath%"

start %~dp0HeyHttp.exe http server
start %~dp0HeyHttp.exe https server -Thumbprint "07261b17e0d71247b185234335c6126bc2796b6b"
start %~dp0HeyHttp.exe https server -Thumbprint "07261b17e0d71247b185234335c6126bc2796b6b" -ClientCertificate 8080
