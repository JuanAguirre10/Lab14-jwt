IF OBJECT_ID('Usuarios', 'U') IS NOT NULL DROP TABLE Usuarios;
GO

CREATE TABLE Usuarios (
    Id        INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    Correo    NVARCHAR(100) NOT NULL CONSTRAINT UQ_Usuarios_Correo UNIQUE,
    ClaveHash NVARCHAR(100) NOT NULL,
    Rol       NVARCHAR(20)  NOT NULL
);
GO

INSERT INTO Usuarios (Correo, ClaveHash, Rol) VALUES
    (N'recepcion@veterinaria.pe',   N'$2a$11$LxHylero7bIwssRTUvbKFu2GK14Xi7fgj5Onr2QsdTB61fSR2qu7u', N'Recepcionista'),
    (N'veterinario@veterinaria.pe', N'$2a$11$wtqgC/4AZ.We9QxyNL3EnezQ5SfcbvKDNTAohrdxEiBacnRF4vKDu', N'Veterinario');
GO

SELECT Id, Correo, Rol FROM Usuarios;
