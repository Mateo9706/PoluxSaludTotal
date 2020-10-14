using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Samico.Models
{
    public class QnAListCreate
    {
        public List<QnaQuestionModel> QnaModelLists { get; set; }

        public QnAListCreate()
        {
            QnaModelLists = new List<QnaQuestionModel>();
        }

    }

    public class QnAListCreateDB
    {
        public List<QnaQuestionModel> QnaModelLists { get; set; }

        public QnAListCreateDB()
        {
            QnaModelLists = new List<QnaQuestionModel>();
        }

    }

    public class GetListFiles
    {
        public List<getFileList> GetListFilesAdd { get; set; }

        public GetListFiles()
        {
            GetListFilesAdd = new List<getFileList>();
        }

    }

    public class QnAListEdit
    {
        public List<QnaQuestionModel> QnaModelListEdit { get; set; }

        public QnAListEdit()
        {
            QnaModelListEdit = new List<QnaQuestionModel>();
        }
    }

    /// <summary>
    /// Class with Question QnA
    /// </summary>
    public class QnaQuestionModel
    {
        public int IdQna { get; set; }

        [Display(Name = "Pregunta")]
        public string Pregunta { get; set; }

        [Display(Name = "Respuesta")]
        public string Respuesta { get; set; }

        public List<string> Preguntas { get; set; }

        public int MetadataSelect { get; set; }

        public string Metadata { get; set; }

        public string SO { get; set; }


        // Edit 

        public string RespuestaE { get; set; }

        public List<string> PreguntasE { get; set; }

        public int MetadataSelectE { get; set; }


    }

    public class getFileList
    {
        public string url { get; set; }  
        public string name { get; set; }
        public string extension { get; set; }
    }
}