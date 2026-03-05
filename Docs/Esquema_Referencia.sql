/* ESTE ES UN SCRIPT DE REFERENCIA PARA LA IA.
   El objetivo es que Entity Framework genere tablas idénticas a esta estructura.
*/

-- 1. EMPRESAS (Centros de Práctica)
CREATE TABLE CentrosPractica (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RazonSocial NVARCHAR(200) NOT NULL,
    Nit VARCHAR(50) NULL,
    Direccion NVARCHAR(300),
    EstadoConvenio NVARCHAR(20) DEFAULT 'Activo' -- Enum: Activo, Inactivo
);

-- 2. PERIODOS ACADÉMICOS
CREATE TABLE Periodos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(20) NOT NULL, -- Ej: 'II-2025'
    FechaInicio DATETIME NOT NULL,
    FechaFin DATETIME NOT NULL,
    PermitirExtemporaneos BIT DEFAULT 0 -- El switch global
);

-- 3. ESTUDIANTES (Vinculado a Identity User)
CREATE TABLE Estudiantes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL, -- FK a AspNetUsers
    CodigoEstudiante VARCHAR(50) NOT NULL, -- RU
    Carrera NVARCHAR(50) NOT NULL, -- INB, PSP, INS...
    EmailInstitucional NVARCHAR(100) NOT NULL, -- Debe ser @ucb.edu.bo
    EstadoAcademico NVARCHAR(20) DEFAULT 'Habilitado' -- Habilitado, Reprobado
);

-- 4. TUTORES INSTITUCIONALES (Vinculado a Identity User)
CREATE TABLE TutoresInstitucionales (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL, -- FK a AspNetUsers
    CentroPracticaId INT NOT NULL, -- FK a CentrosPractica
    Cargo NVARCHAR(100),
    CONSTRAINT FK_Tutor_Centro FOREIGN KEY (CentroPracticaId) REFERENCES CentrosPractica(Id)
);

-- 5. ASIGNACIONES (Tabla Pivote)
CREATE TABLE Asignaciones (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PeriodoId INT NOT NULL,
    EstudianteId INT NOT NULL,
    TutorId INT NOT NULL,
    Estado NVARCHAR(20) DEFAULT 'Pendiente', -- Pendiente, Completado, NoHabilitado
    CONSTRAINT FK_Asig_Periodo FOREIGN KEY (PeriodoId) REFERENCES Periodos(Id),
    CONSTRAINT FK_Asig_Estudiante FOREIGN KEY (EstudianteId) REFERENCES Estudiantes(Id),
    CONSTRAINT FK_Asig_Tutor FOREIGN KEY (TutorId) REFERENCES TutoresInstitucionales(Id)
);

-- 6. FORMULARIO B (Empresa -> Estudiante) - CABECERA
CREATE TABLE EvaluacionesEmpresa (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AsignacionId INT UNIQUE NOT NULL, -- Solo 1 evaluación por asignación
    HorasTrabajadas DECIMAL(10,2) NOT NULL, -- INPUT CRÍTICO
    ScoreTecnicoBruto INT NOT NULL, -- Se multiplicará x2 en reporte
    ScorePowerSkillsBruto INT NOT NULL, -- Se multiplicará x1 en reporte
    FortalezasTexto NVARCHAR(MAX),
    AreasMejoraTexto NVARCHAR(MAX),
    FechaEnvio DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_EvalEmp_Asignacion FOREIGN KEY (AsignacionId) REFERENCES Asignaciones(Id)
);

-- 7. FORMULARIO B - GRILLA DE TAREAS (Detalle 1 a N)
CREATE TABLE EvaluacionEmpresa_Tareas (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EvaluacionEmpresaId INT NOT NULL,
    DescripcionTarea NVARCHAR(500),
    AspectosPositivos NVARCHAR(500),
    AspectosMejorar NVARCHAR(500),
    CONSTRAINT FK_Tarea_Eval FOREIGN KEY (EvaluacionEmpresaId) REFERENCES EvaluacionesEmpresa(Id)
);

-- 8. FORMULARIO A (Estudiante -> Entorno) - CABECERA
CREATE TABLE EvaluacionesEstudiante (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AsignacionId INT UNIQUE NOT NULL,
    ScoreCentroBruto INT NOT NULL, -- x1
    ScoreTutorInstBruto INT NOT NULL, -- x2
    ScoreTutorAcadBruto INT NOT NULL, -- x1
    FortalezasCentro NVARCHAR(MAX),
    LimitacionesCentro NVARCHAR(MAX),
    FortalezasTutor NVARCHAR(MAX),
    LimitacionesTutor NVARCHAR(MAX),
    RecomendacionesCentro NVARCHAR(MAX),
    RecomendacionesTutor NVARCHAR(MAX),
    FechaEnvio DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_EvalEst_Asignacion FOREIGN KEY (AsignacionId) REFERENCES Asignaciones(Id)
);