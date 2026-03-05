using System.ComponentModel.DataAnnotations;

namespace SGPP.Domain.Enums;

public enum Carrera
{
    [Display(Name = "Administración de Empresas")]
    ADM,
    [Display(Name = "Administración Turística")]
    ADT,
    [Display(Name = "Arquitectura")]
    ARQ,
    [Display(Name = "Ciencias Políticas y Relaciones Internacionales")]
    CPO,
    [Display(Name = "Comunicación Digital Multimedia")]
    CDM,
    [Display(Name = "Comunicación Social")]
    COM,
    [Display(Name = "Contaduría Pública")]
    CPA,
    [Display(Name = "Derecho")]
    DER,
    [Display(Name = "Diseño Digital")]
    DIG,
    [Display(Name = "Diseño Gráfico y Comunicación Visual")]
    DGR,
    [Display(Name = "Economía")]
    ECO,
    [Display(Name = "Economía e Inteligencia de Negocios")]
    EIN,
    [Display(Name = "Ingeniería Ambiental")]
    IAM,
    [Display(Name = "Ingeniería Biomédica")]
    INB,
    [Display(Name = "Ingeniería Civil")]
    CIV,
    [Display(Name = "Ingeniería Comercial")]
    ICO,
    [Display(Name = "Ingeniería de Sistemas")]
    SIS,
    [Display(Name = "Ingeniería en Innovación Empresarial")]
    IIE,
    [Display(Name = "Ingeniería en Logística y Analítica de la Cadena de Suministros")]
    IGL,
    [Display(Name = "Ingeniería en Multimedia e Interactividad Digital")]
    IMU,
    [Display(Name = "Ingeniería en Telecomunicaciones")]
    TEL,
    [Display(Name = "Ingeniería Industrial")]
    IND,
    [Display(Name = "Ingeniería Mecatrónica")]
    IMT,
    [Display(Name = "Ingeniería Química")]
    IQM,
    [Display(Name = "Marketing y Medios Digitales")]
    MKD,
    [Display(Name = "Medicina")]
    MED,
    [Display(Name = "Nutrición Clínica y Dietética")]
    NDI,
    [Display(Name = "Psicología")]
    PSI,
    [Display(Name = "Psicopedagogía")]
    PSP,
    [Display(Name = "Otra")]
    OTHER
}
