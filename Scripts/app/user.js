// Función para saber el sistema operativo del usuario
var OSName = "Unknown OS";
if (navigator.appVersion.indexOf("Win") != -1) OSName = "Windows";
if (navigator.appVersion.indexOf("Mac") != -1) OSName = "MacOS";

// Por cada 25 Minutos, S@MI Cierra la conversación.

var getUrlParameter = function getUrlParameter(sParam) {
    var sPageURL = window.location.search.substring(1),
        sURLVariables = sPageURL.split('&'),
        sParameterName,
        i;

    for (i = 0; i < sURLVariables.length; i++) {
        sParameterName = sURLVariables[i].split('=');

        if (sParameterName[0] === sParam) {
            return sParameterName[1] === undefined ? true : decodeURIComponent(sParameterName[1]);
        }
    }
};
// Funciones HUBS

$(function () {
    // Configuración para evitar almacenamiento de caché en IE (AJAX)

    $.ajaxSetup({ cache: false });
    requestDesktopNotificationPermission();

    // Declaraciones de un proxy para hacer referencia al concentrador.

    var chat = $.connection.SamiChatHub;
    var chatObj = {};
    var msgCounter = 0;

    // Se crea la función donde se puede llamar al HUB para la interacción de los mensajes.

    chat.client.addChatMessage = function (chatMsg) {
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
        else
            seconds = second;

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
            $("#Messages").append(Mustache.render(senderMsg, chatObj));
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
                    $("#Messages").append(Mustache.render(receiverMsg, chatObj));
                    notifyMessage();
                    break;
                case 4:
                    const receiverMsg2 = "<br/><div class='text-white' id='cloudSami'><div class='row'><div class='cloud-chat-sami'><span class='post'>{{{Message}}}</span></div></div></div><div class='text-right'><small style='color: #848080; font-size: 12px;'>Enviado por {{Name}} el " + myToday + "</small></div>";
                    //Receiver message bubble template -- Mensaje de S@MI
                    $("#Messages").append(Mustache.render(receiverMsg2, chatObj));
                    notifyMessage();
                    break;
            }
        }

        //Validation to change connected user pic from bot to agent when an agent connects to chat
        if (chatObj.Name !== $("#DisplayName").val()) {
            $("#AgentName").text(chatObj.Name);
            $("#AgentAvatar").attr("src", chatObj.ProfilePictureLocation);
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
    };

    var autoAgent = getUrlParameter('autoAgent');

    // Start the connection.
    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        if (autoAgent === "true") {
            AutoQueueForAgentChatAuto();
            buttons();
        } else {
            chat.server.welcome();
            buttons();
        }

    });

    //Handle enter key on Message text area
    $("#Message").keypress(function (e) {
        if (e.which === 13) {
            if ($("#Message").val() !== "") {
                // Call the Send method on the hub.
                chat.server.sendChatMessage({
                    Message: $("#Message").val(),
                    Name: $("#DisplayName").val(),
                    Group: $.connection.hub.id,
                    ConnectionId: $.connection.hub.id,
                    RepliedByBot: $("#RepliedByBot").val(),
                    AttendedByAgent: $("#AttendedByAgent").val(),
                    ReportCa: $("#ReportCa").val(),
                    OS: OSName
                });
                e.preventDefault();
                // Clear text box and reset focus for next comment.
                $("#Message").val("").focus();
            }
        }
    });

    function buttons() {
        // Buttom
        $("<a />", { html: "<span /> <span style='color:black; font-size: 14px'><i class='fa fa-download'></i></span>", href: "/Chat/GenerateHistory/" + $.connection.hub.id, class: "btn btn-sami-second btn-xs", style: "border-radius: 50%; border: 1px", title: "Generar Historial" }).appendTo($("#HistoryDiv"));
        $("<a />", { html: "<span /> <i class='fa fa-refresh' aria-hidden='true' style='color: black;'></i> <span style='color:white'></span>", class: "btn btn-sami-second", style: "border-radius: 50%; border: 1px", onclick: "window.location.reload()", title: "Recargar Página" }).appendTo($("#HistoryDiv"));
    }

    //Handle Send button click
    $("#Send").click(function () {
        if ($("#Message").val() !== "") {
            // Call the Send method on the hub.
            chat.server.sendChatMessage({
                Message: $("#Message").val(),
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id,
                RepliedByBot: $("#RepliedByBot").val(),
                AttendedByAgent: $("#AttendedByAgent").val(),
                OS: OSName
            });
            // Clear text box and reset focus for next comment.
            $("#Message").val("").focus();
        }
    });

    //Function to handle "accept answer" button from "accept or switch" prompt form
    $(document.body).on("click",
        ".DisconnectChat",
        function () {
            //Stop hub connection
            $.connection.hub.stop();
            alert("Gracias por contactar con Pólux, que estes muy bien.");
            //Refresh window
            window.location.href = "/";
        });

    //Función para hacer otra pregunta
    $(document.body).on("click",
        ".OtherAnswer",
        function () {
            //Disable message text area and sendiing button
            // Start the connection.
            $.connection.hub.logging = true;
            $.connection.hub.start().done(function () {
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "OtherQuery", $.connection.hub.id);

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
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "NegativeAnswer", $.connection.hub.id);

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
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "NegativeAnswer2", $.connection.hub.id);

            });
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".NegativeAnswer2").prop("disabled", true);
            $(".QueueForAgentChat").prop("disabled", true);

        });

    //Función generar Ticket y no fue exitosa
    $(document.body).on("click",
        ".GenerateTicket",
        function () {

            $.connection.hub.logging = true;
            $.connection.hub.start().done(function () {
                chat.server.cloudAfterAnswerSami($("#DisplayName").val(), "CreateNewTicket", $.connection.hub.id);

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

    setTimeout(function () {
        $("#Message").prop("disabled", true);
        $("#Send").prop("disabled", true);
        $(".QueueForAgentChat").prop("disabled", true);
        $(".NegativeAnswer").prop("disabled", true);

        // Cerrar Iteracción con S@MI después de 25 min
        chat.server.closeChatSami({
            Name: $("#DisplayName").val(),
            Group: $.connection.hub.id,
            ConnectionId: $.connection.hub.id
        });

    }, 1000 * 60 * 30);

    function setTime() {
        setTimeout(function () {
            $("#Message").prop("disabled", true);
            $("#Send").prop("disabled", true);
            $(".QueueForAgentChat").prop("disabled", true);
            $(".NegativeAnswer").prop("disabled", true);

            // Cerrar Iteracción con S@MI después de 25 min
            chat.server.closeChatSami({
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id
            });

        }, 1000 * 60 * 30);

    }

    $(document.body).on("click", ".Seguir_Conversando", function () {
        $.connection.hub.logging = true;
        $.connection.hub.start().done(function () {
            chat.server.nextQuery();
            $('#Message').prop('disabled', false);
            $('#Send').prop('disabled', false);
            setTime();
        });
    });

    $(document.body).on("click", ".CancelarConversacion", function () {
        $.connection.hub.logging = true;
        $.connection.hub.start().done(function () {
            chat.server.cancelarConversacion({
                Name: $("#DisplayName").val(),
                Group: $.connection.hub.id,
                ConnectionId: $.connection.hub.id
            });
            $('#Message').prop('disabled', true);
            $('#Send').prop('disabled', true);
        });
    });

    //When user closes the chat window, disconnect from hub
    window.onbeforeunload = function () {
        $.connection.hub.stop();
    }
});

function AutoQueueForAgentChatAuto() {

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
    chat.server.createAgentAutoRequest({
        Name: $("#DisplayName").val(),
        Group: $.connection.hub.id,
        ConnectionId: $.connection.hub.id,
        TypeBoolResolved: 0
    });

    setTimeout(function () { nextQuery2(); }, 60000);
}


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

    setTimeout(function () { nextQuery2(); }, 60000);
}


function nextQuery() {

    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    requestDesktopNotificationPermission();

    // Declare a proxy to reference the hub.
    var chat = $.connection.SamiChatHub;
    var chatObj = {};
    var msgCounter = 0;
    //Disable message text area and sendiing button
    // Start the connection.
    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        chat.server.nextQuery();
        $('#Message').prop('disabled', false);
        $('#Send').prop('disabled', false);
    });
}

function nextQuery2() {
    console.log("Aqui si entra");
    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    requestDesktopNotificationPermission();

    // Declare a proxy to reference the hub.
    var chat = $.connection.SamiChatHub;
    var chatObj = {};
    var msgCounter = 0;
    //Disable message text area and sendiing button
    // Start the connection.
    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        chat.server.nextQuery2({
            Name: $("#DisplayName").val(),
            Group: $.connection.hub.id,
            ConnectionId: $.connection.hub.id,
            TypeBoolResolved: 0
        });
        $('#Message').prop('disabled', false);
        $('#Send').prop('disabled', false);
    });
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

    var es_chrome = navigator.userAgent.toLowerCase().indexOf('chrome') > -1;
    var edge = navigator.userAgent.indexOf("Edge") > -1;
    var firefox = navigator.userAgent.toLowerCase().indexOf('firefox') > -1;
    //var ie = navigator.userAgent.indexOf("MSIE 11") > -1;
    if (es_chrome || edge || firefox) {
        //alert("El navegador que se está utilizando es Chrome");
        if (text.includes("He generado el historial de nuestra conversación:"))
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
    else {
        //alert("El navegador que se está utilizando es Internet Explorer");
        var resp = text.indexOf("He generado el historial de nuestra conversación")
        var resp2 = text.indexOf("en que puedo ser util")
        var resp3 = text.indexOf("Nuestros Agentes se encuentran ocupados, ¿deseas seguir esperando ? o me autorizas para cancelar la petición y enviar el caso al correo de nuestros agentes")
        if (resp2)
            if (resp !== -1) {
                return 1;
            }
            else {
                return 4;
            }
        if (resp2)
            if (resp2 !== -1) {
                return 1;
            }
            else {
                return 2;
            }
        if (resp2)
            if (resp2 !== -1) {
                return 1;
            }
            else {
                if ($('#validatortimeOutAgent').hasClass('timeOutAgent1'))
                    return 3;
            }
    }





    /*if (text.includes("He generado el historial de nuestra conversación:"))
        return 1;
    else if (text.includes("en que puedo ser util"))
        return 2;
    else if (text.includes("Nuestros Agentes se encuentran ocupados, ¿deseas seguir esperando? o me autorizas para cancelar la petición y enviar el caso al correo de nuestros agentes"))
        if ($('#validatortimeOutAgent').hasClass('timeOutAgent1'))
            return 3;
        else
            return 1;
    else
        return 4;*/
}


function activateAlertSami() {
    $('<audio id="chatAudio"><source src="../Sounds/alert-sami.mp3" type="audio/mpeg"></audio>').appendTo('body');
    $('#chatAudio')[0].play();
}

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