namespace SGRVBackEnd.Helpers
{
    public class ApiResponse <T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }= string.Empty; public T? Datos { get; set; }
        public List<string>? Errores { get; set; }
        public static ApiResponse<T> Correcto(T datos, string mensaje= "Operación realizada correctamente")
        {
            return new ApiResponse<T>
            {
                Exito = true,Mensaje = mensaje,Datos = datos
            }
            ;
        }
        public static ApiResponse<T> Fallido(string mensaje, List<string>? errores = null)
        {
            return new ApiResponse <T>
            {
                Exito = false,Mensaje = mensaje,Errores = errores
            }
            ;
        }
    }
}