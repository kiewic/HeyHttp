How to convert a PFX file to Base64?

    X509Certificate2 certificate2 = new X509Certificate2("foo.pfx", String.Empty, X509KeyStorageFlags.Exportable);
    Console.WriteLine(Convert.ToBase64String(certificate2.Export(X509ContentType.Pfx)));

Install a certificate and private key (even it seems to work, there is something missing with the proviate key once it is installed):

    X509Certificate2 certificate = new X509Certificate2(
        "foo.pfx",
        (string)null,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

    var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadWrite);
    store.Add(certificate);
    store.Close();
