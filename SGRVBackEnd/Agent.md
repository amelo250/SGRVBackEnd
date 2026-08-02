# AGENTS.md

## Proyecto

API REST para un sistema de gestión de rent car.

Tecnologías principales:

- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger
- Arquitectura por Controllers, DTOs, Models, Data y Helpers

## Reglas obligatorias

1. No modificar modelos existentes sin autorización explícita.
2. No modificar AppDbContext sin autorización explícita.
3. No crear ni modificar migraciones.
4. No generar scripts SQL ni modificar la base de datos.
5. No cambiar nombres de propiedades existentes.
6. No eliminar endpoints existentes.
7. No modificar contratos de DTOs existentes sin autorización.
8. Mantener compatibilidad con el código actual.
9. Antes de modificar código, explicar qué archivos serán afectados.
10. Ejecutar compilación después de cada implementación.
11. Corregir únicamente errores relacionados con la tarea solicitada.
12. No realizar refactorizaciones generales no solicitadas.

## Convenciones

- Usar async/await.
- Usar ActionResult o IActionResult según el patrón existente.
- Utilizar ApiResponse para respuestas estandarizadas.
- Mantener los DTOs separados de los modelos.
- Aplicar autorización por roles cuando corresponda.
- Mantener nombres en español si el módulo existente está en español.
- No introducir nuevas dependencias sin autorización.

## Validación

Para considerar una tarea terminada:

1. El proyecto debe compilar.
2. No deben aparecer errores nuevos.
3. Los endpoints deben quedar documentados en Swagger.
4. Debe entregarse un resumen de archivos modificados.
5. Deben indicarse pruebas realizadas y resultados.