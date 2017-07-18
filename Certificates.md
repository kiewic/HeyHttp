Root Certificate
================

Create a certificate to act as your Root Certification Authority.

    makecert -n "CN=tempRootCa" -r -sv tempRootCa.pvk tempRootCa.cer

Arguments:

* -n specifies the subject name for the root CA.
* -r specifies that the certificate will be self-signed.
* -sv specifies the file that will contain the private key of the certificate. This will allow the creation of
   certificates using the private key file for signing and key generation.
* tempRootCa.cer is the name of the file containing the public key of the certificate. It does not contain the the
  private key.

In the Create Private Key Password dialog, enter a password and remember it, e.g., abracadabra. Optionally,
you can click None without entering a password.

Install in the Trusted Root CA store (without private key, but private key is not needed to trust other certificates signed with this certificate):

    certutil -addstore -user root tempRootCa.cer

Without `-user`, certificates are installed in *local computer* location instead of *current user* location.

To install in the Personal/My store (without private key), use the `my` option:

    certutil -addstore -user my tempRootCa.cer

Client Certificate
==================

Only create:

    makecert -n "CN=tempClientCert" ^
    -ic tempRootCa.cer -iv tempRootCa.pvk -sky signature ^
    -eku 1.3.6.1.5.5.7.3.2 -sv tempClientCert.pvk tempClientCert.cer

Create and install:

    makecert -n "CN=tempClientCert" ^
    -ic tempRootCa.cer -iv tempRootCa.pvk -sky signature ^
    -sr currentuser -ss my -pe ^
    -eku 1.3.6.1.5.5.7.3.2 -sk MyKeyName

Arguments:

* -sk specifies the key container name for the certificate.
* -n specifies the subject name for the certificate.
* -ic specifies the file containing the root CA certificate.
* -iv specifies the private key file.
* -sr specifies the store location where the certificate will be installed, currentUser or localMachine
* -ss specifies the store name for the certificate.
* -sky specifies the key type, which could be either signature or exchange. Using signature makes the certificate
  capable of signing and enables certificate authentication.
* -pe specifies that the private key is generated in the certificate and installed with it in the certificate store.
  If the certificate does not have the corresponding private key, it cannot be used for certificate authentication.
* `-eku 1.3.6.1.5.5.7.3.2` indicates the certificate is intended for client authentication

Source: http://msdn.microsoft.com/en-us/library/ff650751.aspx


Server Certificate
==================

Only create:

    makecert -n CN="%COMPUTERNAME%" -b 01/01/2010 -e 12/31/2019 ^
    -ic tempRootCa.cer -iv tempRootCa.pvk -sky exchange ^
    -eku 1.3.6.1.5.5.7.3.1 -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12 ^
    -sv %COMPUTERNAME%.pvk %COMPUTERNAME%.cer

Create and install:

    makecert -n CN="%COMPUTERNAME%" -b 01/01/2010 -e 12/31/2019 ^
    -ic tempRootCa.cer -iv tempRootCa.pvk -sky exchange ^
    -eku 1.3.6.1.5.5.7.3.1 -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12 ^
    -sr currentuser -ss my -pe ^
    %COMPUTERNAME%.cer

Convert certificate and private key into a PFX file (resulting file is not protected by password):

    pvk2pfx -pvk %COMPUTERNAME%.pvk -spc %COMPUTERNAME%.cer -pfx %COMPUTERNAME%.pfx

Import certificate and private key into 'my' store:

    certutil -user -importPFX my %COMPUTERNAME%.pfx

Arguments:

* -sky specifies the key type. Using exchange makes the certificate capable of encrypt session keys.
* -eku specifies a enhanced key usage:
** 1.3.6.1.5.5.7.3.1 is id_kp_serverAuth, and it indicates that the certificate can be used as an SSL server certificate.

SPC Certificates
================

Create a code-signing (SPC) certificate:

    makecert -pe -n "CN=Happy SPC" -a sha256 ^
    -cy end -sky signature ^
    -ic tempRootCa.cer -iv tempRootCa.pvk ^
    -sv HappySPC.pvk HappySPC.cer

Convert certificate and private key into a PFX file (resulting file is not protected by password):

    pvk2pfx -pvk HappySPC.pvk -spc HappySPC.cer -pfx HappySPC.pfx

Import certificate and private key into 'my' store:

    certutil -user -importPFX my HappySPC.pfx


Delete Certificate
==================

Use:

    certutil -user -delstore my "tempRootCa"


Browse Installed Certificates
=============================

Use:

1. For current user certificates execute `certmgr.msc`
2. For local computer certificates execute `certlm.msc`

Or:

1. Execute mmc.exe
2. Click File > Add/Remove Snap In ... > Certificates > Add > 
3. Select User or Computer
