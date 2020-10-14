$(".editQandA").on("click", function (event) {

    event.preventDefault();

    var id = $("#IdQna").val();

    var Respuestas = $("#RespuestaE").val();

    var Metadata = $('input[name=metadataE]:checked').val();

    var values = $('input[name="PreguntasE[]"]').map(function () {
        return this.value;
    }).get();

    alert(PublishQnAButton.LinkRedirectController);
    $.ajax({
        beforeSend: function (objeto) {
            alertify.confirm().close();
            alertify.notify("Un momento, estamos publicando todos los registros de QnA Maker");
        },
        type: 'POST',
        data: { "IdQna": id, "PreguntasE": values, "RespuestaE": Respuestas, "MetadataSelectE": Metadata},
        url: PublishQnAButton.LinkSaveEdit,
        success: function (data) {
            if (data.success) {
                alertify.success(data.message);
                location.reload();
            }else
                alertify.danger(data.message);
        },
        error: function (errormessage) {
            alert(errormessage.responseJSON);
        }
    });
    return false;
});


$(document).ready(function () {
    var maxField = 10; //Input fields increment limitation
    var addButton = $('.add_button_edit'); //Add button selector
    var wrapper = $('.queryInputDynamicEdit'); //Input field wrapper
    var fieldHTML = '<div class="input-group quitAll" id="quitEdit" style="padding-bottom:1px;"><input type="text" name="PreguntasE[]" id="PreguntasE" value="" class="form-control" placeholder="Intencion-Entidad"/><span class="input-group-btn"><a href="javascript:void(0);" class="remove_button_edit btn btn-danger" title="Remove field"><i class="fa fa-close"></i></span></div>'; //New input field html 
    var x = 1; //Initial field counter is 1
    $(addButton).click(function () { //Once add button is clicked
        if (x < maxField) { //Check maximum number of input fields
            x++; //Increment field counter
            $(wrapper).append(fieldHTML); // Add field html
        }
    });
    $(wrapper).on('click', '.remove_button_edit', function (e) { //Once remove button is clicked
        e.preventDefault();
        $('#quitEdit').remove(); //Remove field html
        x--; //Decrement field counter
    });
});