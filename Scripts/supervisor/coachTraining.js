// Función para saber el sistema operativo del usuario
var OSName = "Unknown OS";
if (navigator.appVersion.indexOf("Win") != -1) OSName = "Windows";
if (navigator.appVersion.indexOf("Mac") != -1) OSName = "MacOS";

var SOSelect = "";

function button(so) {

    if (so == 'Windows') {
        SOSelect = "";
        $("#btn_windows").addClass('btn-sami-success');
        $("#btn_macos").removeClass('btn-sami-success');
        $("#btn_all").removeClass('btn-sami-success');
        SOSelect = "Windows-";
    }
    if (so == 'Mac') {
        SOSelect = "";
        $("#btn_windows").removeClass('btn-sami-success');
        $("#btn_macos").addClass('btn-sami-success');
        $("#btn_all").removeClass('btn-sami-success');
        SOSelect = 'MacOS-';
    }
    if (so == 'All') {
        SOSelect = "";
        $("#btn_windows").removeClass('btn-sami-success');
        $("#btn_macos").removeClass('btn-sami-success');
        $("#btn_all").addClass('btn-sami-success');
        SOSelect = "";
    }

}

function getListFile() {
    $.ajax({
        beforeSend: function (objeto) {
            $('#botonesDinamicos').html('');
        },
        type: 'GET',
        url: PublishQnAButton.GetListFiles,
        success: function (data) {
            // Html encode display name and message.
            const vm = JSON.parse(data);

            console.log(vm);

            // Empty list users
            $(".list-files").empty();


            vm.forEach(function (listFiles) {

                var url = listFiles.url;
                var name = listFiles.name;
                var extension = listFiles.extension;
                var extensionName = "";

                console.log(extension);

                if (extension === "png" || extension === "jpeg" || extension === "jpg" || extension === "gif" || extension === "PNG")
                    extensionName = "Imagenes";
                else
                    extensionName = "Documentos";

                // Botón para agregar
                var buttonAdd = $('<button data-toggle="tooltip" data-placement="top" title="Agregar" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-plus"></i></button>').click(
                    function () {

                        var finalUrl = "";

                        if (extensionName === "Imagenes") {
                            finalUrl = '![alt text][logo]<br/>[logo]: ' + url + ' "' + name+ '"';
                        }
                        else {
                            finalUrl = '['+ name + '](' + url + ')';
                        }
                        CKEDITOR.instances['Respuesta'].setData(finalUrl)
                    });

                var listPrintFile = "<div class='row'><div class='col-4'> " + extensionName + " </div><div class='col-7'> " + name + "</div><nav class='col-1'></nav></div><br/>";

                $(".list-files").append(Mustache.render(listPrintFile, listFiles));
                $(".list-files nav:last").append('<nav class="col-12"></nav>').find("nav:last").append(buttonAdd);

            });
        }

    });

}

function getListAllQnA() {

    $.ajax({
        beforeSend: function (objeto) {
            $('#botonesDinamicos').html('');
        },
        type: 'GET',
        url: PublishQnAButton.GetListQnA,
        success: function (data) {

            // Html encode display name and message.
            const vm = JSON.parse(data);

            console.log(vm);

            // Empty list users
            $(".listado_qna").empty();


            vm.forEach(function (listApiQnA) {

                var preguntas = [];
                var preguntasHidden = [];
                var preguntasGet = [];
                var id = listApiQnA.IdQna;
                var respuesta = listApiQnA.Respuesta;
                var metadata = listApiQnA.Metadata;
                var metadataValue = "";

                if (metadata === "")
                    metadataValue = "Sin Filtro";
                else
                    metadataValue = metadata;

                listApiQnA.Preguntas.forEach(function (questions) {
                    preguntas.push("<p style='border: 1px solid #ccc; padding: 9px 10px 10px 14px; '>" + questions + "</p>");
                });

                listApiQnA.Preguntas.forEach(function (questions) {
                    preguntasHidden.push("'" + questions + "'");
                });

                listApiQnA.Preguntas.forEach(function (questions) {
                    preguntasGet.push(questions);
                });

                // Botón para editar
                var buttonEdit = $('<button data-toggle="tooltip" data-placement="top" title="Editar Usuario" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-edit"></i></button>').click(
                    function () {
                        var datas = JSON.stringify(preguntasGet);
                        $.ajax({
                            type: 'GET',
                            url: PublishQnAButton.LinkGetData,
                            data: { "Preguntas": datas },
                            success: function (response) {
                                $("#editQnASelect").html(response);
                                $('ul.nav.nav-tabs a:eq(1)').html('Editar');
                                $('ul.nav.nav-tabs a:eq(1)').tab('show');
                            }
                        });
                    });


                // Botón para Eliminar
                var buttonDelete = $('<button data-toggle="tooltip" data-placement="top" title="Eliminar" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-remove"></i></button>').click(
                    function () {
                        $.ajax({
                            type: 'POST',
                            url: PublishQnAButton.LinkDelete + "/" + id,
                            contentType: "application/json;charset=UTF-8",
                            dataType: "json",
                            success: function (response) {
                                if (response.success) {
                                    alertify.success(response.message);
                                    getListAllQnA();
                                } else {
                                    alertify.error(response.message);
                                }
                            }
                        });
                    });

                var listQnAPrint = "<div class='row'><div class='col-4'>" + "<div class='panel panel-default'><div class='panel-body'>" + preguntas.join("") + "</div></div>" + "</div><div class='col-4 markdown-text'><div>" + respuesta + "</div></div><div class='col-3'>" + metadataValue + "</div> " + "<div class='col-1'><nav class='btn-group inline pull-right'></nav></div></div> </div><hr style='width: 100%; color: black; height: 1px; background-color:#3C1053;'/>";

                $(".listado_qna").append(Mustache.render(listQnAPrint, listApiQnA));
                $(".listado_qna nav:last").append('<nav class="col-12"></nav>').find("nav:last").append(buttonDelete, buttonEdit);
                var listQnAPrintNone = "<tr><td>" + preguntasHidden + "</td><td>" + respuesta + "</td><td>" + metadata + "</td></tr>";
                $(".listado_qna_hidden").append(Mustache.render(listQnAPrintNone, listApiQnA));

                $(".markdown-text > div").markdown({
                    target_form: ".markdown-target"
                });

                $(".markdown-text > div > p > a").addClass('btn btn-sami-success').attr('target', '_blank');
                $(".markdown-text > div > p > a > img").addClass('img-responsive').removeClass('btn btn-sami-success').attr('width', '100%');

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

            });
        }
    })

}
// Función Botones de Windows y MAC

$(function () {

    getListAllQnA();
    getListFile();
    // //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });
    requestDesktopNotificationPermission();

    var coachHub = $.connection.CoachHub;
    var chatObj = {};
    var msgCounter = 0;

    coachHub.client.addChatMessageAlert = function (chatMsg) {
        chatObj = chatMsg;
        console.log("Mensaje: " + chatMsg);
    }

    coachHub.client.addChatMessage2 = function (chatMsg) {
        chatObj = chatMsg;
        var today = new Date();
        var month = today.getUTCMonth() + 1;
        var minute = today.getMinutes();
        var day = today.getUTCDate();
        var hour = today.getHours();
        var second = today.getSeconds();
        var minutes = "";
        var months = "";
        var days = "";
        var hours = "";
        var seconds = "";

        if (second < 10)
            seconds = 0 + "" + second;

        if (minute < 10)
            minutes = 0 + "" + minute;
        else
            minutes = minute;

        if (month < 10)
            months = 0 + "" + month;
        else
            months = month;

        if (day < 10)
            days = 0 + "" + day;
        else
            days = day;

        if (hour < 10)
            hours = 0 + "" + hour;
        else
            hours = hour;

        // Display the month, day, and year. getMonth() returns a 0-based number.
        var myToday = today.getUTCFullYear() + "-" + months + "-" + days + " " + hours + ":" + minutes + ":" + seconds;
        // Nombre y mensaje de codificación HTML.
        const encodedName = htmlEncode(chatObj.Name);

        /*
         * Add the message to the message container.
         * Messages will be rendered with Mustache library from each respective template + chat object
         * for more info: https://github.com/janl/mustache.js
         */

        if (encodedName === htmlEncode($("#DisplayName").val())) {
            //Sender message bubble template -- Mensaje del Usuario
            const senderMsg = "<br/><div class='text-white' id='user-chat'><div class='row'><div class='cloud-chat-user' style='text-align: right;'><span class='post'>{{{Message}}}</span></div></div></div><div class='text-right'><small style='color: #848080;'>Enviado por {{Name}} el " + myToday + "</small></div>";
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $("#Messages2").append(Mustache.render(senderMsg, chatObj));
        } else {
            var numCase = NumCaseSamiAnswer(chatObj.Message);
            switch (numCase) {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    const receiverMsg = "<br/><div class='text-white' id='cloudSami'><div class='row'><div class='cloud-chat-sami'><span class='post'>{{{Message}}}</span></div></div></div><div class='text-right'><small style='color: #848080; font-size: 12px;'>Enviado por {{Name}} el " + myToday + "</small></div>";
                    //Receiver message bubble template -- Mensaje de S@MI
                    $("#Messages2").append(Mustache.render(receiverMsg, chatObj));
                    notifyMessage();
                    break;
                case 4:
                    const receiverMsg2 = "<br/><div class='text-white' id='cloudSami'><div class='row'><div class='cloud-chat-sami'><span class='post'>{{{Message}}}</span></div></div></div><div class='text-right'><small style='color: #848080; font-size: 12px;'>Enviado por {{Name}} el " + myToday + "</small></div>";
                    //Receiver message bubble template -- Mensaje de S@MI
                    $("#Messages2").append(Mustache.render(receiverMsg2, chatObj));
                    notifyMessage();
                    break;
            }
        }

        //Validation to change connected user pic from bot to agent when an agent connects to chat
        if (chatObj.Name !== $("#DisplayName").val()) {
            $("#AgentName").text(chatObj.Name);
            $("#AgentAvatar").attr("src", "/Images/UploadedProfilePictures/" + chatObj.ProfilePictureLocation);
        }

        //Set flag values from chat object 
        $("#RepliedByBot").val(chatObj.RepliedByBot);
        $("#AttendedByAgent").val(chatObj.AttendedByAgent);

        //If chat has been attended by agent, enable message text area and send button
        if (chatObj.AttendedByAgent) {
            $("#Message").prop("disabled", false);
            $("#Send").prop("disabled", false);
        }
        //Otherwise, system has sent accept or switch to agent form and user is replying, thus message sending must be disabled until they answer the prompt


        //Scroll messages container to bottom after a messsage has been added
        const msgContainer = $("#msgContainer");

        msgContainer.scrollTop(msgContainer.prop("scrollHeight"));
        msgCounter++;
        
    }

    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        coachHub.server.welcome2();
    });

    //Handle enter key on Message text area
    $("#Message").keypress(function (e) {
        if (e.which === 13) {
            if ($("#Message").val() !== "") {
                // Call the Send method on the hub.
                coachHub.server.sendChatMessage2({
                    Message: $("#Message").val(),
                    Name: $("#DisplayName").val(),
                    Group: $.connection.hub.id,
                    ConnectionId: $.connection.hub.id,
                    RepliedByBot: $("#RepliedByBot").val() === "true",
                    AttendedByAgent: $("#AttendedByAgent").val() === "true",
                    OS: OSName
                });
                e.preventDefault();
                // Clear text box and reset focus for next comment.
                $("#Message").val("").focus();
            }
        }
    });

    $("#Send").click(function () {
        if ($("#Message").val() !== "") {
            // Call the Send method on the hub.
            coachHub.server.sendChatMessage2({
                Message: $("#Message").val(),
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id,
                RepliedByBot: $("#RepliedByBot").val() === "true",
                AttendedByAgent: $("#AttendedByAgent").val() === "true",
                OS: OSName
            });
            // Clear text box and reset focus for next comment.
            $("#Message").val("").focus();
        }
    });

    /*
     * FUNCIONES PARA AGREGAR, MODIFICAR, ELIMINAR PREGUNTAS, RESPUESTAS Y METADATAS.
     * 
     */

    

    //Función para hacer otra pregunta
    $(document.body).on("click",
        ".OtherAnswer",
        function () {
            //Disable message text area and sendiing button
            // Start the connection.
            $.connection.hub.logging = true;
            $.connection.hub.start().done(function () {
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "OtherQuery");

            });
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".GenerateTicket").prop("disabled", true);
            $(".OtherAnswer").prop("disabled", true);

        });

    //Funtion not get agent
    $(document.body).on("click",
        ".NegativeAnswer",
        function () {
            //Disable message text area and sendiing button
            // Start the connection.
            $.connection.hub.logging = true;
            $.connection.hub.start().done(function () {
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "NegativeAnswer");

            });
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".NegativeAnswer").prop("disabled", true);
            $(".QueueForAgentChat").prop("disabled", true);

        });


    $(document.body).on("click",
        ".NegativeAnswer2",
        function () {
            //Disable message text area and sendiing button
            // Start the connection.
            $.connection.hub.logging = true;
            $.connection.hub.start().done(function () {
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "NegativeAnswer");

            });
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".NegativeAnswer").prop("disabled", true);
            $(".QueueForAgentChat").prop("disabled", true);

        });

    //Función generar Ticket y no fue exitosa
    $(document.body).on("click",
        ".GenerateTicket",
        function () {

            $.connection.hub.logging = true;
            $.connection.hub.start().done(function () {
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "CreateNewTicket");

            });
            //Disable message text area and sendiing button
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".GenerateTicket").prop("disabled", true);
            $(".OtherAnswer").prop("disabled", true);
        });


    //Function to handle "switch to agent" button from "accept or switch" prompt form
    $(document.body).on("click",
        ".QueueForAgentChat",
        function () {
            //Disable message text area and sendiing button
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".QueueForAgentChat").prop("disabled", true);
            $(".NegativeAnswer").prop("disabled", true);
            //Create agent request
            chat.server.createAgentRequest({
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id,
                TypeBoolResolved: 1
            });
        });

    //Function to handle "Switch to agent Again" 
    $(document.body).on("click",
        ".QueueForAgentChatAgain",
        function () {
            //Disable message text area and sendiing button
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".QueueForAgentChatAgain").prop("disabled", true);
            $(".CancelRequest").prop("disabled", true);
            //Create agent request
            chat.server.createAgentRequestAgain({
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id
            });

            setTimeout(function () { nextQuery2(); }, 60000);
        });

    //Function to Cancel Request
    $(document.body).on("click",
        ".CancelRequest",
        function () {
            //Disable message text area and sendiing button
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".QueueForAgentChatAgain").prop("disabled", true);
            $(".CancelRequest").prop("disabled", true);
            //Create agent request
            chat.server.deleteRequestAgent({
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id
            });
        });

    //When user closes the chat window, disconnect from hub
    window.onbeforeunload = function () {
        $.connection.hub.stop();
    }

    //When user closes the chat window, disconnect from hub
    window.onbeforeunload = function () {
        $.connection.hub.stop();
    }

});

$(document).ready(function () {
    var maxField = 10; //Input fields increment limitation
    var addButton = $('.add_button'); //Add button selector
    var wrapper = $('.queryInputDynamic'); //Input field wrapper
    var c = 0;
    
    //var fieldHTML = '<div class="input-group quitAll" id="quit" style="padding-bottom:1px;"><span class="input-group-btn"><div id="raddioButtons"></div></span><input type="text" name="Preguntas[]" id="Preguntas" value="" class="form-control" placeholder="Intencion-Entidad"/><span class="input-group-btn"><a href="javascript:void(0);" class="remove_button btn btn-danger" title="Remove field"><i class="fa fa-close"></i></span></div>'; //New input field html 
    var x = 1; //Initial field counter is 1
    $(addButton).click(function () { //Once add button is clicked
        if (x < maxField) { //Check maximum number of input fields
            sessionStorage.setItem("PreguntasArray", $("#Preguntas").val());
            sessionStorage.setItem("SOArray", SOSelect);
            var fieldHTML = '<div class="input-group quitAll" id="quit" style="padding-bottom:1px;"><input type="text" name="PreguntasSave[]" id="PreguntasSave[]" value="' + sessionStorage.getItem("SOArray") + sessionStorage.getItem("PreguntasArray") + '" class="form-control" placeholder="Intencion-Entidad"/><span class="input-group-btn"><a href="javascript:void(0);" class="remove_button btn btn-danger" title="Remove field"><i class="fa fa-close"></i></span></div>';
            x++; //Increment field counter
            $(wrapper).append(fieldHTML); // Add field html
            $("#btn_windows").removeClass('btn-sami-success');
            $("#btn_macos").removeClass('btn-sami-success');
            $("#btn_all").removeClass('btn-sami-success');
            $("#Preguntas").val("");
        }
    });
    $(wrapper).on('click', '.remove_button', function (e) { //Once remove button is clicked
        e.preventDefault();
        $('#quit').remove(); //Remove field html
        x--; //Decrement field counter
    });
});


function AutoQueueForAgentChat() {

    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    requestDesktopNotificationPermission();

    // Declare a proxy to reference the hub.
    var chat = $.connection.SamiChatHub;
    var chatObj = {};
    var msgCounter = 0;
    //Disable message text area and sendiing button
    $("#Message").prop("disabled", true);
    $("#Send").prop("disabled", true);
    //Create agent request
    chat.server.createAgentRequest({
        Name: $("#DisplayName").val(),
        Group: $.connection.hub.id,
        ConnectionId: $.connection.hub.id,
        TypeBoolResolved: 0
    });

    setTimeout(function () { nextQuery4(); }, 60000);
}


function nextQuery3() {

    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    requestDesktopNotificationPermission();

    // Declare a proxy to reference the hub.
    var coachHub = $.connection.CoachHub;
    var chatObj = {};
    var msgCounter = 0;
    //Disable message text area and sendiing button
    // Start the connection.
    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        
        coachHub.server.nextQuery();
        $('#Message').prop('disabled', false);
        $('#Send').prop('disabled', false);
    });
}

function nextQuery4() {
    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    requestDesktopNotificationPermission();

    // Declare a proxy to reference the hub.
    var coachHub = $.connection.CoachHub;
    var chatObj = {};
    var msgCounter = 0;
    //Disable message text area and sendiing button
    // Start the connection.
    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        coachHub.server.nextQuery2({
            Name: $("#DisplayName").val(),
            Group: $.connection.hub.id,
            ConnectionId: $.connection.hub.id,
            TypeBoolResolved: 0
        });
        $('#Message').prop('disabled', false);
        $('#Send').prop('disabled', false);
    });
}

function activateAlertSami() {
    document.getElementById("samiAudioAlert").play();
}


function requestDesktopNotificationPermission() {
    if ("Notification" in window) {
        if (Notification && Notification.permission === 'default') {
            Notification.requestPermission(function (permission) {
                if (!('permission' in Notification)) {
                    Notification.permission = permission;
                }
            });
        }
    } else
        console.log("Notification API not supported");
}

function notifyMessage() {
    var text = "¡Tienes un nuevo mensaje!";
    if (!document.hasFocus()) {
        if ("Notification" in window) {
            if (Notification.permission === "granted") {
                activateAlertSami();
                this.sendDesktopNotification(text);

            } else {
                activateAlertSami();
                this.sendDesktopNotification(text);
            }
        } else {
            activateAlertSami();
            this.sendDesktopNotification(text);
        }
    }
}

function NumCaseSamiAnswer(text) {

    if (text.includes("He generado tu historial para que el agente se entere de nuestra conversación"))
        return 1;
    else if (text.includes("en que puedo ser util"))
        return 2;
    else if (text.includes("Nuestros Agentes se encuentran ocupados, ¿deseas seguir esperando? o me autorizas para cancelar la petición y enviar el caso al correo de nuestros agentes"))
        if ($('#validatortimeOutAgent').hasClass('timeOutAgent1'))
            return 3;
        else
            return 1;
    else
        return 4;
}

$(".publicQnA").on("click", function () {
    $.ajax({
        beforeSend: function (objeto) {
            alertify.confirm().close();
            alertify.notify("Un momento, estamos publicando todos los registros de QnA Maker");
        },
        type: 'POST',
        url: PublishQnAButton.LinkRedirectController,
        success: function (data) {
            if (data.success)
                alertify.success(data.message);
            else
                alertify.danger(data.message);
        },
        error: function (errormessage) {
            alert(errormessage.responseJSON);
        }
    });
    return false;

});



$(".addNewQandA").on("click", function (event) {

    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    event.preventDefault();

    var Respuestas = CKEDITOR.instances['Respuesta'].getData();
    //var Respuestas = $('#Respuesta').val();

    var Metadata = $('input[name=metadata]:checked').val();

    var values = $('input[name="PreguntasSave[]"]').map(function () {
        return this.value;
    }).get();

    $.ajax({
        type: "POST",
        url: PublishQnAButton.LinkSave,
        data: { "Preguntas": values, "Respuesta": Respuestas, "MetadataSelect": Metadata, "SO": SOSelect },
        success: function (data) {
            if (data.success) {
                alertify.success(data.message);
                getListAllQnA();
                setTimeout(
                    function () {
                        $("#tab").pagination({
                            items: 10,
                            contents: 'listado_qna',
                            previous: 'Previous',
                            next: 'Next',
                            position: 'bottom',
                        });
                    }, 10000);
            }else
                alertify.danger(data.message);
        },
        error: function (errormessage) {
            alert(errormessage.responseJSON);
        }
    });

    $('#FormularioQnaPost')[0].reset();
    $('.quitAll').remove();
});
//Send Notification
function sendDesktopNotification(text) {
    var notification = new Notification("Pólux", {
        icon: "/Images/UploadedProfilePictures/bot-avatar.png",
        body: text,
        tag: "s@mi"
    });
    //’tag’ handles muti tab scenario i.e when multiple tabs are 
    // open then only one notification is sent
    //3. handle notification events and set timeout 
    notification.onclick = function () {
        parent.focus();
        window.focus(); //just in case, older browsers
        window.document.getElementById("Message").focus();
        this.close();
    };
}



//Helper functions
function htmlEncode(val) {
    return $("<div />").text(val).html();
}

function htmlDecode(html) {
    const txt = document.createElement("textarea");
    txt.innerHTML = html;
    return txt.value;
}


window.onload = function () {
    document.getElementById('uploader').onsubmit = function () {
        var formdata = new FormData(); //FormData object
        var fileInput = document.getElementById('fileInput');
        //Iterating through each files selected in fileInput
        for (i = 0; i < fileInput.files.length; i++) {
            //Appending each file to FormData object
            formdata.append(fileInput.files[i].name, fileInput.files[i]);
        }
        //Creating an XMLHttpRequest and sending
        var xhr = new XMLHttpRequest();
        xhr.open('POST', '/Coach/UploadFile');
        xhr.send(formdata);
        xhr.onreadystatechange = function () {
            if (xhr.readyState == 4 && xhr.status == 200) {
                alert(xhr.responseText);
            }
        }
        return false;
    }
}