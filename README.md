# Veterinaria

Web API REST para registrar a los propietarios de una veterinaria y a sus mascotas. Está hecha con ASP.NET Core (.NET 10), Entity Framework Core con **base de datos primero** y AutoMapper. La base `VeterinariaDB` se crea con los scripts `db/VeterinariaDB.sql` y `db/Usuarios.sql`, y el modelo se genera con Scaffold.

La API está protegida con **JWT**. El usuario inicia sesión, recibe un token y lo envía en cada petición con el encabezado `Authorization: Bearer <token>`. Según el rol que tenga dentro del token, puede o no usar cada endpoint.

La solución está separada en cuatro proyectos. Los controladores solo trabajan con servicios y DTOs; nunca usan el `DbContext` ni las entidades.

## Capas

| Proyecto | Contenido | Referencia a |
|---|---|---|
| `Veterinaria.Entidades` | Entidades (`Propietario`, `Mascota`, `Usuario`) y los DTOs en `Dtos/` | — |
| `Veterinaria.Datos` | `VeterinariaContext` y los repositorios con sus interfaces | Entidades |
| `Veterinaria.Negocio` | Servicios con las reglas de negocio, `AuthServicio` (registro, login y token), `JwtOpciones`, `MappingProfile` de AutoMapper y las excepciones | Datos |
| `Veterinaria.Api` | Controladores, `Program.cs` (JWT, Swagger y CORS) y las pruebas `.http` | Negocio |

Los repositorios devuelven entidades y no saben nada de DTOs ni de HTTP. Los servicios validan las reglas, convierten entre DTOs y entidades con AutoMapper y devuelven DTOs. Todos los métodos son asíncronos y las consultas se hacen con LINQ.

## Entidades

| Entidad | Campos |
|---|---|
| `Propietario` | Id, Nombres (máx. 100), Dni (8 dígitos, único), Telefono (opcional, 9 dígitos que empiezan con 9), Correo (opcional, máx. 100) |
| `Mascota` | Id, Nombre (máx. 50), Especie (máx. 30), Raza (opcional, máx. 50), FechaNacimiento, Peso (decimal(5,2)), PropietarioId |
| `Usuario` | Id, Correo (máx. 100, único), ClaveHash (hash BCrypt, nunca la clave), Rol (máx. 20) |

Relación 1:N: un propietario tiene muchas mascotas y cada mascota pertenece a un propietario (`FK_Mascotas_Propietarios`). En la base, un mismo propietario no puede tener dos mascotas con el mismo nombre (`UQ_Mascotas_PropietarioId_Nombre`).

`Usuario` se agregó a mano en `Veterinaria.Entidades` y en `VeterinariaContext` (índice único `UQ_Usuarios_Correo`), para no tener que volver a generar todo con Scaffold.

Las entidades no tienen `[JsonIgnore]` ni Data Annotations porque nunca salen de la API: lo que se recibe y se devuelve son DTOs.

## DTOs

| DTO | Uso | Detalle |
|---|---|---|
| `PropietarioDto` | Respuesta | Incluye `CantidadMascotas` (`MapFrom` de `Mascotas.Count`) |
| `PropietarioCrearDto` | POST | Nombres, Dni, Telefono, Correo |
| `PropietarioActualizarDto` | PUT | Igual que el de crear, pero sin Dni porque el DNI no cambia |
| `MascotaDto` | Respuesta | Incluye `Propietario` con el nombre del dueño (`MapFrom` de `Propietario.Nombres`) |
| `MascotaCrearDto` | POST | Nombre, Especie, Raza, FechaNacimiento, Peso, PropietarioId |
| `MascotaActualizarDto` | PUT | Igual que el de crear, pero sin PropietarioId: una mascota no cambia de dueño |
| `RegistroDto` | Registro | Correo y Clave (entre 6 y 50 caracteres). No tiene rol: el rol lo pone el servidor |
| `LoginDto` | Login | Correo y Clave |
| `TokenDto` | Respuesta del login | Token y fecha en que expira |
| `PerfilDto` | Respuesta del perfil | Correo y Rol, leídos de los claims del token |

Ningún DTO de entrada lleva Id; el id va en la ruta. Las validaciones son Data Annotations (`[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]`) con mensajes en español. Con `[ApiController]`, si los datos no son válidos la API responde 400 automáticamente.

Todos los mapeos están en `Veterinaria.Negocio/MappingProfile.cs`. No se usa `ReverseMap` porque cada mapeo se usa en un solo sentido. En Swagger, la sección Schemas muestra los DTOs que usan los endpoints.

## Reglas de negocio (409)

- No se puede registrar un propietario con un DNI que ya existe.
- No se puede eliminar un propietario que todavía tiene mascotas.
- Un propietario no puede tener dos mascotas con el mismo nombre (POST y PUT de mascotas).
- No se puede registrar un usuario con un correo que ya existe.

Los servicios lanzan `ReglaNegocioException` y el controlador la convierte en un 409 con `ProblemDetails`.

También responden 400, con el error en el campo que corresponde:

- POST de mascota con un `propietarioId` que no existe (`PropietarioId`).
- POST o PUT de mascota con fecha de nacimiento futura (`FechaNacimiento`).

## Seguridad con JWT

- `POST /api/auth/registro` crea el usuario. La clave se guarda como hash con BCrypt y el rol siempre es **Recepcionista**; el cliente no puede elegirlo.
- `POST /api/auth/login` revisa el correo y la clave. Si están bien devuelve el token; si el correo no existe o la clave está mal, responde el mismo 401, así no se sabe cuál de los dos falló.
- El token se firma con HS256 y lleva los claims `sub` (id del usuario), `email` y el rol. Dura 60 minutos.
- `GET /api/auth/perfil` devuelve el correo y el rol del usuario leyendo los claims del token, sin ir a la base.
- En `Program.cs`, `AddJwtBearer` valida el emisor, la audiencia, la fecha de expiración y la firma. `UseAuthentication` va antes de `UseAuthorization`.
- Los controladores tienen `[Authorize]` a nivel de clase, `[AllowAnonymous]` en las consultas públicas de mascotas y `[Authorize(Roles = "...")]` en las acciones de un solo rol. Los 401 y 403 los responde el middleware, no los controladores.
- La clave de firma y los demás datos del JWT están en los secretos de usuario, igual que la cadena de conexión. No están en `appsettings.json` ni en el repositorio.

## Roles

| Rol | Quién es | Qué puede hacer además de lo básico |
|---|---|---|
| **Recepcionista** | Atiende a los clientes en el mostrador | Eliminar propietarios |
| **Veterinario** | Atiende a las mascotas | Editar y eliminar mascotas |

Los dos pueden ver propietarios, registrar propietarios y mascotas, y editar los datos de un propietario. Cualquier persona, sin iniciar sesión, puede ver la lista de mascotas y el detalle de una mascota.

## Matriz de permisos

| Endpoint | Sin token | Recepcionista | Veterinario |
|---|---|---|---|
| `POST /api/auth/registro` | Sí | Sí | Sí |
| `POST /api/auth/login` | Sí | Sí | Sí |
| `GET /api/auth/perfil` | No (401) | Sí | Sí |
| `GET /api/mascotas` | Sí | Sí | Sí |
| `GET /api/mascotas/{id}` | Sí | Sí | Sí |
| `POST /api/mascotas` | No (401) | Sí | Sí |
| `PUT /api/mascotas/{id}` | No (401) | No (403) | Sí |
| `DELETE /api/mascotas/{id}` | No (401) | No (403) | Sí |
| `GET /api/propietarios` | No (401) | Sí | Sí |
| `GET /api/propietarios/{id}` | No (401) | Sí | Sí |
| `GET /api/propietarios/{id}/mascotas` | No (401) | Sí | Sí |
| `POST /api/propietarios` | No (401) | Sí | Sí |
| `PUT /api/propietarios/{id}` | No (401) | Sí | Sí |
| `DELETE /api/propietarios/{id}` | No (401) | Sí | No (403) |

401 quiere decir que no se envió token o que no es válido. 403 quiere decir que el token es válido pero el rol no tiene permiso.

## Usuarios de prueba

`db/Usuarios.sql` crea la tabla `Usuarios` y dos usuarios, uno por rol:

| Correo | Clave | Rol |
|---|---|---|
| `recepcion@veterinaria.pe` | `Clave123` | Recepcionista |
| `veterinario@veterinaria.pe` | `Clave123` | Veterinario |

En la tabla solo está el hash BCrypt de la clave. Los hashes se obtuvieron registrando los dos usuarios con `POST /api/auth/registro`; después se cambió el rol del segundo con un `UPDATE` y se copiaron los hashes al script.

## Endpoints

| Verbo | Ruta | Respuesta |
|---|---|---|
| POST | `/api/auth/registro` | 201, 400, 409 |
| POST | `/api/auth/login` | 200, 400, 401 |
| GET | `/api/auth/perfil` | 200, 401 |
| GET | `/api/propietarios?buscar=` | 200 |
| GET | `/api/propietarios/{id}` | 200, 404 |
| GET | `/api/propietarios/{id}/mascotas` | 200, 404 |
| POST | `/api/propietarios` | 201, 400, 409 |
| PUT | `/api/propietarios/{id}` | 204, 400, 404 |
| DELETE | `/api/propietarios/{id}` | 204, 404, 409 |
| GET | `/api/mascotas?especie=&propietarioId=&buscar=` | 200 |
| GET | `/api/mascotas/{id}` | 200, 404 |
| POST | `/api/mascotas` | 201, 400, 409 |
| PUT | `/api/mascotas/{id}` | 204, 400, 404, 409 |
| DELETE | `/api/mascotas/{id}` | 204, 404 |

En propietarios, `buscar` filtra por nombres o DNI. En mascotas, `buscar` filtra por nombre o raza, y los tres filtros se pueden combinar. A esas respuestas se suman el 401 y el 403 de la matriz de permisos. Las pruebas de cada endpoint y de cada código están en `Veterinaria.Api/Veterinaria.Api.http`; se ejecutan en orden sobre la base recién cargada (los dos scripts). Los dos logins guardan el token en `@tokenRecepcion` y `@tokenVeterinario`, y las demás peticiones los usan en el encabezado `Authorization`.

## Ejecutar en local (Visual Studio)

1. En SSMS, conectarse a `.\SQLEXPRESS`, crear la base `VeterinariaDB` y ejecutar sobre ella `db/VeterinariaDB.sql` y después `db/Usuarios.sql`.
2. Abrir `Veterinaria.slnx`.
3. Clic derecho en `Veterinaria.Api` > **Administrar secretos de usuario** y pegar lo siguiente, cambiando `Clave` por un texto propio de al menos 32 caracteres:

   ```json
   {
     "ConnectionStrings": {
       "Veterinaria": "Server=.\\SQLEXPRESS;Database=VeterinariaDB;Integrated Security=True;TrustServerCertificate=True"
     },
     "Jwt": {
       "Clave": "ESCRIBE-AQUI-UNA-CLAVE-DE-32-CARACTERES-O-MAS",
       "Emisor": "Veterinaria.Api",
       "Audiencia": "Veterinaria.Clientes",
       "Minutos": 60
     }
   }
   ```

4. Dejar `Veterinaria.Api` como proyecto de inicio y ejecutar con F5 (perfil `http`). Se abre Swagger en http://localhost:5213/swagger.
5. En Swagger, ejecutar `POST /api/auth/login` con uno de los usuarios de prueba, copiar el `token`, pulsar **Authorize** y pegarlo. Desde ese momento Swagger envía el token en cada petición.

Desde la terminal, en la carpeta de la solución:

```bash
sqlcmd -S ".\SQLEXPRESS" -E -C -Q "CREATE DATABASE VeterinariaDB"
sqlcmd -S ".\SQLEXPRESS" -E -C -d VeterinariaDB -i db/VeterinariaDB.sql
sqlcmd -S ".\SQLEXPRESS" -E -C -d VeterinariaDB -i db/Usuarios.sql
dotnet user-secrets set "ConnectionStrings:Veterinaria" "Server=.\SQLEXPRESS;Database=VeterinariaDB;Integrated Security=True;TrustServerCertificate=True" --project Veterinaria.Api
dotnet user-secrets set "Jwt:Clave" "ESCRIBE-AQUI-UNA-CLAVE-DE-32-CARACTERES-O-MAS" --project Veterinaria.Api
dotnet user-secrets set "Jwt:Emisor" "Veterinaria.Api" --project Veterinaria.Api
dotnet user-secrets set "Jwt:Audiencia" "Veterinaria.Clientes" --project Veterinaria.Api
dotnet user-secrets set "Jwt:Minutos" "60" --project Veterinaria.Api
dotnet run --project Veterinaria.Api --launch-profile http
```

Cada script borra y vuelve a crear sus tablas, así que también sirven para regresar a los datos iniciales (primero `VeterinariaDB.sql` y luego `Usuarios.sql`). La cadena de conexión y los datos del JWT no están en `appsettings.json`; se leen de los secretos de usuario. Si falta la sección `Jwt`, la API no arranca y lo indica.

## Scaffold

Se ejecuta sobre `Veterinaria.Datos`, con `Veterinaria.Api` como proyecto de inicio para que lea la cadena de los secretos de usuario. `dotnet-ef` está en el manifiesto local `dotnet-tools.json`:

```bash
dotnet tool restore
dotnet ef dbcontext scaffold "Name=ConnectionStrings:Veterinaria" Microsoft.EntityFrameworkCore.SqlServer --project Veterinaria.Datos --startup-project Veterinaria.Api --output-dir Models --context VeterinariaContext --no-onconfiguring --force
```

Consola del Administrador de paquetes:

```
Scaffold-DbContext "Name=ConnectionStrings:Veterinaria" Microsoft.EntityFrameworkCore.SqlServer -Project Veterinaria.Datos -StartupProject Veterinaria.Api -OutputDir Models -Context VeterinariaContext -NoOnConfiguring -Force
```

El Scaffold deja todo en `Veterinaria.Datos/Models`. Después se movieron `Propietario.cs` y `Mascota.cs` a `Veterinaria.Entidades` (namespace `Veterinaria.Entidades`) y `VeterinariaContext.cs` a la raíz de `Veterinaria.Datos` (namespace `Veterinaria.Datos`, con `using Veterinaria.Entidades;`), y se borró la carpeta `Models`. Además, el Scaffold nombra la colección de `Propietario` como `Mascota`, en singular, así que se renombró a `Mascotas` en la entidad y en el `WithMany` del contexto. Si se vuelve a generar, hay que repetir estos pasos y volver a dejar `Usuario` como está ahora.

## Estructura

```
Veterinaria/
├── Veterinaria.slnx
├── dotnet-tools.json
├── db/                VeterinariaDB.sql, Usuarios.sql
├── Veterinaria.Entidades/
│   ├── Propietario.cs, Mascota.cs, Usuario.cs
│   └── Dtos/          PropietarioDto, PropietarioCrearDto, PropietarioActualizarDto,
│                      MascotaDto, MascotaCrearDto, MascotaActualizarDto,
│                      RegistroDto, LoginDto, TokenDto, PerfilDto
├── Veterinaria.Datos/
│   ├── VeterinariaContext.cs
│   └── Repositorios/  IPropietarioRepositorio, PropietarioRepositorio,
│                      IMascotaRepositorio, MascotaRepositorio,
│                      IUsuarioRepositorio, UsuarioRepositorio
├── Veterinaria.Negocio/
│   ├── MappingProfile.cs, JwtOpciones.cs
│   ├── ReglaNegocioException.cs, DatoInvalidoException.cs
│   └── Servicios/     IPropietarioServicio, PropietarioServicio,
│                      IMascotaServicio, MascotaServicio,
│                      IAuthServicio, AuthServicio
└── Veterinaria.Api/
    ├── Controllers/   AuthController, PropietariosController, MascotasController
    ├── Program.cs     EF Core, repositorios, servicios, AutoMapper, JWT, Swagger y CORS
    └── Veterinaria.Api.http
```

CORS solo permite el origen `http://localhost:5173`.

## Paquetes

| Proyecto | Paquete | Versión |
|---|---|---|
| Datos | Microsoft.EntityFrameworkCore.SqlServer | 10.0.14 |
| Negocio | AutoMapper | 16.3.0 |
| Negocio | BCrypt.Net-Next | 4.2.0 |
| Negocio | System.IdentityModel.Tokens.Jwt | 8.23.0 |
| Api | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.14 |
| Api | Microsoft.AspNetCore.OpenApi | 10.0.14 |
| Api | Swashbuckle.AspNetCore.SwaggerUI | 10.3.1 |
