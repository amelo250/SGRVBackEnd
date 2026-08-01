namespace SGRVBackEnd.Helpers
{
    public class ApiResponseHelper <T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }= string.Empty; public T? Datos { get; set; }
        public List<string>? Errores { get; set; }
        public static ApiResponseHelper<T> Correcto(T datos, string mensaje= "Operación realizada correctamente")
        {
            return new ApiResponseHelper<T>
            {
                Exito = true,Mensaje = mensaje,Datos = datos
            }
            ;
        }
        public static ApiResponseHelper<T> Fallido(string mensaje, List<string>? errores = null)
        {
            return new ApiResponseHelper <T>
            {
                Exito = false,Mensaje = mensaje,Errores = errores
            }
            ;
        }
    }
}