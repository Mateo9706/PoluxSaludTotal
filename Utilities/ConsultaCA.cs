using Samico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Samico.Utilities
{
    public class ConsultaCA
    {
        public DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        ///     Consultar el ticket solicitado por un usuario.
        ///     Retorna un string.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public async Task<string> GetTicketCa(string query, int companyId)
        {
            // Se inicializa las variables
            int login = 0;
            string first = "", second = "", third = "", fourth = "", fifth = "", sixth = "", crConsultado = "", groupId = "", resultadoFinal = "";
            // Conexión a la base de datos
            SamiEntities db = new SamiEntities();
            // Consulta parametros de CA
            var connectionCA = (from connCa in db.ConexionCAs where connCa.IdCompania == companyId select connCa).First();
            // Web Service SOAP CA.
            CaConexionSaludTotal.USD_WebServiceSoapClient ca = new CaConexionSaludTotal.USD_WebServiceSoapClient();

            // Se logea por ca
            login = ca.login(connectionCA.UsuarioCA, connectionCA.PasswordCA);
            // Atributos de consulta
            string[] datos = { "status.sym", "assignee.userid", "assignee.combo_name", "description" };
            // Se consulta el cr del caso, el estado, el asignado y la descripción del usuario
            first = ca.doSelect(login, "cr", $"ref_num='{query}'", 1, datos);
            // Convertimos el archivo xml en texto
            XElement firstX = XElement.Parse(first);

            // Se toma solo las etiquetas <Handle>cr:</Handle>
            var att = firstX.Descendants("Handle").ToList();

            // Se guarda el cr extraido.
            foreach (var item in att)
                crConsultado = item.Value;

            // Separamos el id del grupo
            var idGroup = firstX.Descendants("AttrValue").ToList();

            // Se consulta el atributo tipo de solicitud
            string[] datos2 = { "type.sym" };

            // Se consulta por segunda vez para traer el tipo de solicitud, en este caso se ingresa el CR consultado
            second = ca.doSelect(login, "cr", $"persistent_id='{crConsultado}'", 255, datos2);

            // Se convierte la respuesta XML a Texto
            XElement secondX = XElement.Parse(second);

            // Se consulta el atributo de la descripción
            string[] datos3 = { "description" };

            // Consultar el comentario en proceso, trae todos los comentarios concatenados
            third = ca.doSelect(login, "alg", $"call_req_id='{crConsultado}' and type in ('ST','LOG','TR','RE', 'CB', 'RS', 'SOLN', 'ESC', 'NF')", 255, datos3);

            // Se convierte el resultado XML en texto
            XElement thirdX = XElement.Parse(third);

            // Consultar el comentario en proceso, trae todos los comentarios concatenados
            sixth = ca.doSelect(login, "alg", $"call_req_id='{crConsultado}' and type in ('ST','LOG','TR','RE', 'CB', 'RS', 'SOLN', 'ESC', 'NF')", 255, datos3);

            // Se convierte el resultado XML en texto
            XElement sixthX = XElement.Parse(sixth);

            // Consultar el comentario resuelto

            fourth = ca.doSelect(login, "alg", $"call_req_id='{crConsultado}' and type = 'RE'", 255, datos3);

            // Se convierte el resultado XML en texto
            XElement fourthX = XElement.Parse(fourth);

            // Se consulta el atributo de la descripción
            string[] datos5 = { "last_mod_dt" };

            // Consultar el comentario en proceso, trae todos los comentarios concatenados
            fifth = ca.doSelect(login, "cr", $"persistent_id='{crConsultado}'", 255, datos5);

            // Se convierte el resultado XML en texto
            XElement seventh = XElement.Parse(fifth);

            // Se toma solo las etiquetas <AttrValue></AttrValue> del XML
            var finalAtt = firstX.Descendants("AttrValue").ToList();
            var finalAtt2 = secondX.Descendants("AttrValue").ToList();
            var finalAtt3 = thirdX.Descendants("AttrValue").ToList();
            var finalAtt4 = fourthX.Descendants("AttrValue").ToList();
            var finalAtt6 = sixthX.Descendants("AttrValue").ToList();
            var finalAtt7 = seventh.Descendants("AttrValue");

        
            var time = Convert.ToInt64(finalAtt7.First().Value);

            var timeConvert = UnixTimeToDateTime(time);
            // Se crea array de cadena de texto.
            string[] lista = { "<strong>Tipo de Caso:</strong> " };
            string[] lista2 = { "<br/><strong>Estado:</strong> ", "<br/><strong>Caso Asignado a:</strong> ", "<br/><strong>Grupo: </strong>", "<br/><strong>Descripción usuario:</strong> " };
            string[] lista3 = { "<br/><strong>Comentario en Proceso:</strong><br/>" };
            string[] lista4 = { "<br/><strong>Comentario Final:</strong><br/>" };

            // Se combina el resultado de CA con la cadena de Array
            var answerCA = finalAtt2.Zip(lista, (n, w) => new { Number = n, Word = w });
            var answerCA2 = finalAtt.Zip(lista2, (n, w) => new { Number = n, Word = w });
            var answerCA3 = finalAtt3.Zip(lista3, (n, w) => new { Number = n, Word = w });
            var answerCA4 = finalAtt4.Zip(lista4, (n, w) => new { Number = n, Word = w });
            // Tipo de Solicitud
            foreach (var nw in answerCA)
                // Se imprime los resultados junto con la cadena de array, aquí se elimina el <AttrValue></AttrValue>
                resultadoFinal += String.Join(" ", nw.Word.ToString() + nw.Number.Value);

            // Asignado a:, descripción del usuario y el estado del caso.
            foreach (var nw in answerCA2)
                // Se imprime los resultados junto con la cadena de array, aquí se elimina el <AttrValue></AttrValue>
                resultadoFinal += String.Join(" ", nw.Word.ToString() + nw.Number.Value);

            // Se valida si el tipo de solicitud es Resuelto o Cerrado
            if (resultadoFinal.Contains("Resuelto") || resultadoFinal.Contains("Cerrado"))
            {

                // Si tiene el estado Resuelto o Cerrado se consulta el comentario final, hecho por el agente humano
                foreach (var nw in answerCA4)
                {
                    // Se imprime los resultados junto con la cadena de array, aquí se elimina el <AttrValue></AttrValue>
                    resultadoFinal += String.Join(" ", nw.Word.ToString() + nw.Number.Value);
                }

                resultadoFinal += $"<div><strong>Fecha de última actualización:</strong>{timeConvert}</div>";

            }
            else
            {
                if (resultadoFinal.Contains("Abierto")) { }
                else
                {
                    // Si contiene otro estado, como Abierto, Re-Abierto, en proceso, genera la lista de todos los comentarios del proceso
                    resultadoFinal += $"<div><strong>Fecha de última actualización:</strong>{timeConvert}</div>";
                    var data = "";
                    foreach (var nw in finalAtt3)
                        // Validamos si la respuesta fue hecha por el sistema.
                        if (nw.Value.Contains("AHD") || nw.Value.Contains("Transferir"))
                            // Si fue hecha por el sistema, lo ignora y no lo imprime
                            resultadoFinal += "";
                        else
                            // Si el comentario fue hecho por un agente humano, lo imprime.
                            foreach (var item in finalAtt6)
                                data = item.Value;
                    resultadoFinal += data;
                    resultadoFinal += "<br/><strong>Comentario en Proceso:</strong><br/>";
                }

            }


            // Cierra sesión
            ca.logout(login);
            // Retorna la consulta del ticket

            return resultadoFinal;
        }

    }
}