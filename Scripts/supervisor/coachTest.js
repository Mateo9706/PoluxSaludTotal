$(function () {

    // //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    var coachHub = $.connection.CoachHub;  

    coachHub.client.getApiQnAList = function (listQnA) {

        // Html encode display name and message.
        const vm = JSON.parse(listQnA); 

        // Empty list users
        $("#listado_qna").empty();

        vm.forEach(function (listApiQnA) {

            var preguntas = [];
            var preguntasHidden = [];
            var respuestasHidden = "";
            var id = listApiQnA.IdQna;
            var respuesta = listApiQnA.Respuesta;
            var metadata = listApiQnA.Metadata;
            var metadataValue = "";
            var metadataValueHidden = "";

            if (metadata === "")
                metadataValue = "Sin Filtro";
            else
                metadataValue = metadata;

            listApiQnA.Preguntas.forEach(function (questions) {                
                preguntas.push("<p style='border: 1px solid #ccc; padding: 9px 10px 10px 14px; '>" + questions + "</p>");
            });

            listApiQnA.Preguntas.forEach(function (questions) {
                preguntasHidden.push("'"+questions+"'");
            });

            /* Botón para editar
            var buttonEdit = $('<button data-toggle="tooltip" data-placement="top" title="Editar Usuario" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-edit"></i></button>').click(
                function () {
                    Edit('Edit/' + Id + '');
                });
            */

            // Botón para Eliminar
            var buttonDelete = $('<button data-toggle="tooltip" data-placement="top" title="Eliminar" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-remove"></i></button>').click(
                function () {
                    Delete('Delete/' + Id + '');
                });

            var listQnAPrint = "<div class='row'><div class='col-4'>" + "<div class='panel panel-default'><div class='panel-body'>" + preguntas.join("") + "</div></div>" + "</div><div class='col-4'>" + respuesta + "</div><div class='col-3'>" + metadataValue + "</div> " + "<div class='col-1'><nav class='btn-group inline pull-right'></nav></div></div> </div><hr style='width: 100%; color: black; height: 1px; background-color:#3C1053;'/>";

            $(".listado_qna").append(Mustache.render(listQnAPrint, listApiQnA));
            $(".listado_qna nav:last").append('<nav class="col-12"></nav>').find("nav:last").append(buttonDelete);

            var listQnAPrintNone = "<tr><td>" + preguntasHidden + "</td><td>" + respuesta + "</td><td>" + metadata + "</td></tr>";
            $(".listado_qna_hidden").append(Mustache.render(listQnAPrintNone, listApiQnA));
        });

    }    

    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        coachHub.server.getApiQnAList();
    });

   

    /*
     * FUNCIONES PARA AGREGAR, MODIFICAR, ELIMINAR PREGUNTAS, RESPUESTAS Y METADATAS.
     * 
     */


});

setTimeout(
    function () {
        $("#tab").pagination({
            items: 10,
            contents: 'listado_qna',
            previous: 'Previous',
            next: 'Next',
            position: 'bottom',
        });
    }, 2000);


