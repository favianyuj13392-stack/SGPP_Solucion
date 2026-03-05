# MASTER TECHNICAL PLAN: SISTEMA DE GESTIÓN DE PRÁCTICAS PREPROFESIONALES (SGPP)

> **CONTEXTO PARA LA IA:**
> Eres el Arquitecto de Software Principal y Desarrollador Senior .NET asignado a este proyecto. Tu objetivo es construir una plataforma web robusta para la Universidad Católica Boliviana (UCB) que digitalice las evaluaciones de pasantías. Debes priorizar la integridad de los datos, la precisión matemática en los cálculos de ponderación y la fidelidad visual ("pixel-perfect") respecto a los formularios físicos de Excel.
>
> **REGLA DE ORO:** El sistema reemplaza al Excel, pero el output (Reportes) debe ser indistinguible de los reportes históricos generados manualmente.

---

## 1. STACK TECNOLÓGICO & RESTRICCIONES DE INFRAESTRUCTURA

El sistema debe ser nativo para ecosistemas Microsoft Windows Server.

- **Runtime:** .NET 8.0 (LTS) - ASP.NET Core Web App.
- **Hosting:** IIS (Internet Information Services) con ASP.NET Core Hosting Bundle.
- **Patrón de Arquitectura:** Monolito Modular con Clean Architecture (Capas: Domain, Infrastructure, Application, WebUI).
- **Lenguaje:** C# 12.
- **Base de Datos:** SQL Server 2019 o superior.
- **ORM:** Entity Framework Core 8 (Code-First Approach).
- **Frontend Strategy:**
  - **Razor Pages:** Para el renderizado del lado del servidor (SEO, velocidad inicial).
  - **HTMX:** Para interactividad dinámica (CRUD de filas en tablas, autosave) sin la complejidad de React/Angular.
  - **Bootstrap 5:** Framework CSS base.
- **Librerías Críticas:**
  - `ClosedXML`: Para generación de Excel (Reportes).
  - `QuestPDF`: Para generación de certificados/reportes PDF.
  - `FluentValidation`: Para reglas de negocio en formularios.

---

## 2. ESQUEMA DE BASE DE DATOS (ENTITY RELATIONS)

La estructura de datos debe soportar reportes complejos y analítica de empresas.

### 2.1. Módulo Core & Identidad

- **`ApplicationUser : IdentityUser`**
  - `Nombre` (nvarchar 100)
  - `Apellido` (nvarchar 100)
  - `EsActivo` (bool)
- **`Estudiantes`**
  - `Id` (PK)
  - `ApplicationUserId` (FK -> AspNetUsers)
  - `CodigoEstudiante` (varchar 20)
  - `Carrera` (Enum: INB, PSP, INS, ARQ, etc.)
  - `EmailInstitucional` (Validación estricta: debe terminar en `@ucb.edu.bo`)
- **`CentrosPractica` (Empresas - Entidad Clave)**
  - `Id` (PK)
  - `RazonSocial` (nvarchar 200)
  - `Nit` (varchar 50, opcional)
  - `Direccion` (nvarchar 300)
  - `Rubro` (nvarchar 100)
  - `EstadoConvenio` (Enum: Activo, EnRevision, Inactivo)
- **`TutoresInstitucionales`**
  - `Id` (PK)
  - `ApplicationUserId` (FK -> AspNetUsers)
  - `CentroPracticaId` (FK -> CentrosPractica)
  - `Cargo` (nvarchar 100)
  - `TelefonoContacto` (nvarchar 50)

### 2.2. Módulo de Gestión Académica

- **`Periodos`**
  - `Id` (PK)
  - `CodigoGestion` (ej: "II-2025")
  - `FechaInicio` (datetime)
  - `FechaCierre` (datetime) - _Cierre automático._
  - `PermitirExtemporaneos` (bool) - _Switch global de emergencia._
- **`Asignaciones` (Tabla Pivote Central)**
  - `Id` (PK)
  - `PeriodoId` (FK)
  - `EstudianteId` (FK)
  - `TutorInstitucionalId` (FK)
  - `Estado` (Enum: Pendiente, EnProceso, Completado, NoHabilitado)
  - `FechaCreacion` (datetime)
  - _Nota:_ Si `Estado == NoHabilitado`, el estudiante reprueba administrativamente (notas 0).

### 2.3. Módulo de Formularios (Rich Inputs)

- **`FormularioB_Empresa`** (Evaluación al Estudiante)
  - `Id` (PK)
  - `AsignacionId` (FK, Unique)
  - `HorasTrabajadas` (decimal 10,2) - **INPUT MANUAL OBLIGATORIO**
  - `ScoreTecnicoBruto` (int) - _Suma simple._
  - `ScorePowerSkillsBruto` (int) - _Suma simple._
  - `FortalezasTexto` (nvarchar MAX)
  - `AreasMejoraTexto` (nvarchar MAX)
- **`FormularioB_Tareas`** (Grilla 1-N)
  - `Id` (PK)
  - `FormularioBId` (FK)
  - `DescripcionTarea` (nvarchar 500)
  - `AspectosPositivos` (nvarchar 500)
  - `AspectosMejorar` (nvarchar 500)
- **`FormularioB_DetalleRespuestas`**
  - `Id`, `FormularioBId`, `PreguntaKey` (int), `Valor` (1-4), `Justificacion` (nvarchar).
- **`FormularioA_Estudiante`** (Evaluación al Centro)
  - `Id` (PK)
  - `AsignacionId` (FK, Unique)
  - `ScoreCentroBruto` (int)
  - `ScoreTutorInstBruto` (int)
  - `ScoreTutorAcadBruto` (int)
  - `FortalezasCentro`, `LimitacionesCentro` (nvarchar MAX)
  - `FortalezasTutor`, `LimitacionesTutor` (nvarchar MAX)
  - `RecomendacionesCentro`, `RecomendacionesTutor` (nvarchar MAX)
- **`FormularioA_DetalleRespuestas`**
  - `Id`, `FormularioAId`, `PreguntaKey`, `Valor` (1-4), `Justificacion`, `Observaciones`.

---

## 3. LÓGICA DE NEGOCIO & CÁLCULOS (ENGINE)

El sistema debe realizar cálculos invisibles al usuario para generar el reporte final.

### 3.1. Algoritmo de Ponderación (Hard-Coded)

Al guardar o finalizar un formulario, calcular y persistir o proyectar los valores finales:

| Sección (Origen)                   | Input (Preguntas) | Operación Matemática | Output Máximo |
| :--------------------------------- | :---------------: | :------------------: | :-----------: |
| **Form B: Conocimientos Técnicos** | 8 preguntas (1-4) |  `Sum(Valores) * 2`  |    **64**     |
| **Form B: Power Skills**           | 9 preguntas (1-4) |  `Sum(Valores) * 1`  |    **36**     |
| **Form A: Centro Práctica**        | 9 preguntas (1-4) |  `Sum(Valores) * 1`  |    **36**     |
| **Form A: Tutor Institucional**    | 5 preguntas (1-4) |  `Sum(Valores) * 2`  |    **40**     |
| **Form A: Tutor Académico**        | 6 preguntas (1-4) |  `Sum(Valores) * 1`  |    **24**     |

### 3.2. Reglas de Validación de Flujo

1.  **Registro Estudiantes:** Regex estricto `^[a-zA-Z0-9._%+-]+@ucb\.edu\.bo$`. Rechazar cualquier otro dominio.
2.  **Cierre de Periodo:**
    - Si `FechaActual > Periodo.FechaCierre` Y `Periodo.PermitirExtemporaneos == false` -> **Bloquear Edición** (Read Only).
    - Si `Periodo.PermitirExtemporaneos == true` -> **Permitir Edición** a todos los usuarios pendientes (Estado Global).
3.  **Estado "No Habilitado":**
    - Acción manual del Admin.
    - Efecto: El estudiante ya no puede editar formularios. En reportes aparece con 0 en todas las notas y "NO HABILITÓ" en observaciones.

---

## 4. ESPECIFICACIONES DE UI/UX (DETALLADO)

### 4.1. Vista Tutor (Formulario B)

- **Layout:** Single Page Form con validación parcial.
- **Input Horas:** Campo destacado al inicio (`<input type="number" step="0.5">`). Etiqueta: _"Total Horas Trabajadas en el Semestre"_.
- **Grilla de Tareas (HTMX):**
  - Tabla HTML. Footer de tabla tiene botón `[+ Agregar Tarea]`.
  - Al hacer clic, HTMX hace GET a `/FormularioB/RowTemplate` y anexa una nueva fila `<tr>` al `<tbody>`.
  - Límite soft de 10 tareas.
- **Preguntas Cualitativas:**
  - Diseño de fila:
    - Col 1: Texto de la pregunta.
    - Col 2: Radio Buttons (Muy Insatisfecho [1] ... Totalmente Satisfecho [4]).
    - Col 3: Textarea "Justificación" (Visible siempre, 2 líneas de altura inicial).

### 4.2. Dashboard Administrador (Torre de Control)

- **Tabla Maestra:**
  - Filtros: Periodo, Carrera, Centro de Práctica.
  - Columnas: Estudiante, Empresa, Estado Form A, Estado Form B, Nota Preliminar.
- **Columna Acciones:**
  - `[Ver Detalle]`: Abre Modal "Auditoría" o nueva pestaña.
  - `[No Habilitar]`: Toggle Switch o Botón de peligro.
- **Vista Auditoría:**
  - Split View (Pantalla dividida) o Tabs. Izquierda: Formulario A (Solo lectura). Derecha: Formulario B (Solo lectura).
  - Permite al Admin verificar discrepancias (ej. Estudiante dice que trabajó 0 horas, Empresa dice 200).

### 4.3. Analytics de Empresas

- Nueva vista: "Ranking de Centros".
- Lógica: Agrupar `EvaluacionesEstudiante` por `CentroPracticaId`.
- KPIs: Promedio de satisfacción del estudiante, Total de estudiantes recibidos, Semestres activos.

---

## 5. OUTPUTS & REPORTES (CRÍTICO)

### 5.1. Excel Consolidado (Mapping "Anexo 2")

Usar `ClosedXML` para generar un archivo con las siguientes columnas exactas:

1.  **Nro**: Correlativo.
2.  **Carrera**: `Estudiante.Carrera`.
3.  **Nombre Estudiante**: `Usuario.Apellido` + `Usuario.Nombre`.
4.  **Horas Trabajadas**: `FormularioB.HorasTrabajadas` (Si Estado=NoHabilitado -> 0).
5.  **Conocimientos Técnicos**: `FormularioB.ScoreTecnicoBruto * 2`.
6.  **Power Skills**: `FormularioB.ScorePowerSkillsBruto`.
7.  **Nombre Centro de Práctica**: `Centro.RazonSocial`.
8.  **Centro de práctica**: `FormularioA.ScoreCentroBruto`.
9.  **Tutor Institucional**: `FormularioA.ScoreTutorInstBruto * 2`.
10. **Tutor académico**: `FormularioA.ScoreTutorAcadBruto`.

---

## 6. GUÍA DE IMPLEMENTACIÓN PARA LA IA (PASO A PASO)

1.  **Project Setup:** Inicializar solución ASP.NET Core MVC/Razor. Configurar DI container y DbContext con SQL Server.
2.  **Domain Modeling:** Crear las clases de Entidad descritas en la sección 2. Configurar relaciones (One-to-Many) y Enums.
3.  **Database Migration:** Crear migración inicial y script de seed data (Usuario Admin, 1 Periodo, 3 Carreras, 2 Empresas Demo).
4.  **Auth Logic:** Implementar `AccountController` con validación de dominio `@ucb.edu.bo`. Crear pantalla de creación de tutores (Admin only).
5.  **Form B Implementation (Complex):**
    - Crear ViewModel que soporte la lista de tareas.
    - Implementar vista Razor con soporte HTMX para la grilla.
    - Backend: Logic para recibir el POST, validar horas y calcular puntajes (x2).
6.  **Form A Implementation:** Replicar estructura para estudiante.
7.  **Admin Dashboard:** Crear tabla con filtros y lógica de cambio de estado ("No Habilitar").
8.  **Export Engine:** Servicio que consulta `Asignaciones` con `Include()` profundo y mapea a celdas de Excel.
9.  **QA Checks:** Verificar que un estudiante "No Habilitado" no pueda entrar y salga con 0 en el Excel.
