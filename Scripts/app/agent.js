$(function () {

    //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    //Time constants
    const oneMinute = 1000 * 30;
    const fiveMinutes = 1000 * 60;

    // Declare proxies to reference the hubs.
    var notifHub = $.connection.SamiNotificationHub;
    var adminHub = $.connection.SamiAdminCrudHub;
    var chat = $.connection.SamiChatHub;
    var maxTabs = 10, index = 1;

    // Create a function that the hub can call to broadcast messages.
    notifHub.client.getNotifications = function (notification) {
        // Html encode display name and message.
        const notifications = JSON.parse(notification);

        //Clear notifications container
        $("#Notifications").empty();

        //Change color to notifications depending on time elapsed since user request
        notifications.forEach(function (notification) {
            var connId = notification.RequesterGroup;

            const notificationDate = new Date(notification.CreationDate);
            let alertColor = "";
            let messageAlert = "";
            var msg1 = "Necesito de tu ayuda, el usuario te está esperando para ser atendido.";
            var msg2 = "Hay usuarios en espera, por favor atiendelos.";

            if (((new Date) - notificationDate) < oneMinute) {
                messageAlert = "Nuevo usuario en espera.";
                alertColor = "alert-success";
                notifyMessage(msg1);
            } else if (((new Date) - notificationDate) > oneMinute &&
                ((new Date) - notificationDate) < fiveMinutes) {
                alertColor = "alert-warning";
                messageAlert = "Usuario sin ser atendido.";
                notifyMessage(msg2);
            }  else if (((new Date) - notificationDate) > fiveMinutes) {
                alertColor = "alert-danger";
                messageAlert = "El usuario ha esperado más de 1 minuto.";

                setTimeout(function () {
                    chat.server.changeRequest({
                        Name: $("#DisplayName").val(),
                        Group: connId,
                        ConnectionId: connId,
                        RepliedByBot: true,
                        AttendedByAgent: true
                    });
                }, 2000);

            }

            //Notification template
            //language=html
            var notificationTemplate = "<div class='chat_list " + alertColor + "' role='alert' data-request-id='{{RequestId}}' data-connection-id='{{ConnectionId}}' data-requester='{{RequesterName}}' data-group-name='{{RequesterGroup}}' data-request-date-time='{{CreationDate}}'><div class='chat_people'><div class='chat_img'><img src='http://simpleicon.com/wp-content/uploads/user1.png' alt='user_sami_notification'></div><div class='chat_ib'><h5> {{RequesterName}}<span class='chat_date'>{{CreationDate | datetime}}</span></h5><p style='color:#FFFFFF;'>" + messageAlert + "</p></div></div></div>";

            /**
             * Render notification 
             * Messages will be rendered with Mustache library from template + notification object
             * for more info: https://github.com/janl/mustache.js
             */
            $("#Notifications").append(Mustache.render(notificationTemplate, notification));
        });
    };


    // Change Color Notification Alert Automatic in 3 seg
    setInterval(ReloadGetNotification, 360000);

    // Function call getNotification()
    function ReloadGetNotification() {
        notifHub.server.getNotifications();
    }

    function ClickNavQuitClass() {
        if ($('.active').length > 0) {
            // code
        }
    }

    // Create a function that the hub can call to broadcast messages.
    chat.client.addChatMessage = function (chatMsg) {

        var today = new Date();
        var month = today.getUTCMonth() + 1;
        var minute = today.getMinutes();

        // Display the month, day, and year. getMonth() returns a 0-based number.
        var myToday = today.getUTCFullYear() + "-" + month + "-" + today.getUTCDate() + " " + today.getHours() + ":" + minute + ":" + today.getSeconds();

        // Html encode display name and message.
        const encodedName = htmlEncode(chatMsg.Name);

        // History notification


        //Sender message bubble template
        const senderMsg = "<div class='incoming_msg'><div class='received_msg'><div class='received_withd_msg'><div class='text-white'><div class='row'><div class='cloud-chat-sami'><span class='post'>{{{Message}}}</span></div></div></div><span class='time_date'>Enviado por {{Name}} el  " + myToday + "</span></div></div></div>";

        //Receiver message bubble template
        const receiverMsg = "<div class='outgoing_msg'><div class='sent_msg'><div class='text-white' id='user-chat'><div class='row'><div class='cloud-chat-user' style='text-align: right; margin-top: 0px !important; width: 95% !important'><span class='post'>{{{Message}}}</span></div></div></div><span class='time_date'>Enviado por {{Name}} el " + myToday + "</span></div></div>";

        /* 
         * Add the message to the message container.
         * Messages will be rendered with Mustache library from each respective template + chat object
         * for more info: https://github.com/janl/mustache.js
         */
        if (encodedName === htmlEncode($("#DisplayName").val())) {
            $("#Messages-" + chatMsg.ConnectionId).append(Mustache.render(senderMsg, chatMsg));
        } else {
            if ($('#ConnectionID-' + chatMsg.ConnectionId).hasClass('active')) {
                $('#ConnectionID-' + chatMsg.ConnectionId).removeClass("animable");
            } else {
                $('#ConnectionID-' + chatMsg.ConnectionId).addClass("animable");
                notifyMessage(chatMsg.Name);
            }
            $("#Messages-" + chatMsg.ConnectionId).append(Mustache.render(receiverMsg, chatMsg));
        }

        //Scroll messages container to bottom after a messsage has been added
        const msgContainer = $("#msgContainer-" + chatMsg.ConnectionId);
        msgContainer.scrollTop(msgContainer.prop("scrollHeight"));
    };

    $(document.body).on("click", "a[data-connection-id]", function () {
        var connId = $(this).data("connection-id");
        $('#ConnectionID-' + connId).removeClass("animable");
    });


    //Handles notification click
    $(document.body).on("click",
        ".chat_list[data-request-id]",
        function () {

            //Get data from notification alert
            var connId = $(this).data("connection-id");
            const groupName = $(this).data("group-name");
            const requester = $(this).data("requester");
            var name = "";

            index++;

            //Start chat window creation
            $(".tab-toggle").length !== maxTabs ||
                $("#tabs").append("dropTemp")
                    .find("#drop").append($(".li-tab-toggle:last").removeClass("active"));

            //Chat tab template
            const navTemp = "<li class ='nav-item li-tab-toggle' id='msgContainer'><a href='#tab-{{ConnectionId}}' data-connection-id='{{ConnectionId}}' id='ConnectionID-{{ConnectionId}}' class ='nav-link tab-toggle' data-toggle='tab'><button class ='close closeTab' type='button' >×</button><span class ='dev-nr'>{{Name}}</span></a></li>";

            //Chat container template
            const tabTemp = "<div class ='tab-pane' id='tab-{{ConnectionId}}'><div class='mesgs'><div class='container'><div class='msg_history' style='max-height: 330px; overflow-y: auto; height: 500px' id='msgContainer-{{ConnectionId}}'><div id='Messages-{{ConnectionId}}' class ='container'><div class='content'><input type='hidden' id='connectionIDHidden' value='{{ConnectionId}}'/></div></div></div></div><div class='type_msg'><div class='input_msg_write input-group col-12 p-2'><textarea type='text' class='form-control' style='resize: none' placeholder='Escriba su mensaje' id='Message-{{ConnectionId}}' data-connection-id='{{ConnectionId}}'></textarea><button class='btn msg_send_btn' type='button'  id='Send-{{ConnectionId}}' data-connection-id='{{ConnectionId}}'><i class='fa fa-paper-plane-o' aria-hidden='true'></i></button></div></div></div></div>";

            //Render tab and container with mustache
            const nav = Mustache.render(navTemp,
                { ConnectionId: connId, Name: requester, Index: index });
            const tab = Mustache.render(tabTemp,
                { ConnectionId: connId, Name: requester, Index: index });

            //Insert in tab container
            $(nav).insertAfter(".li-tab-toggle:last");
            $(tab).appendTo(".tab-content");
            $(".li-tab-toggle:last a").tab("show");
            //End chat window creation

            //Remove notification
            notifHub.server.removeNotification($(this).data("request-id"));
            //Request notifications
            notifHub.server.getNotifications();

            //Join user chat group
            chat.server.joinGroup(groupName);
            //Notify agent conncetion to user
            chat.server.notifyAgentConnection({
                Name: $("#DisplayName").val(),
                Group: connId,
                ConnectionId: connId,
                RepliedByBot: true,
                AttendedByAgent: true
            });

            //Generate History SAMI
            chat.server.historyUser({
                Name: $(this).data("request-id") + "-" + requester,
                Group: connId,
                ConnectionId: connId,
                RepliedByBot: true,
                AttendedByAgent: true
            });

            notifHub.server.getNotifications();

            //Remove notification from list
            $(this).remove();
        });

    //Handle Send button(s) click
    $(document.body).on("keypress",
        "textarea[id^='Message-']",
        function (e) {
            if (e.which === 13) {
                var conId = $(this).data("connection-id");

                if ($("#Message-" + conId).val() !== "") {
                    // Call the Send method on the hub.
                    chat.server.sendChatMessage({
                        Message: $("#Message-" + conId).val(),
                        Name: $("#DisplayName").val(),
                        Group: conId,
                        ConnectionId: conId,
                        RepliedByBot: true,
                        AttendedByAgent: true,
                        ProfilePictureLocation: $("#ProfilePictureLocation").val()
                    });
                    e.preventDefault();
                    // Clear text box and reset focus for next comment.
                    $("#Message-" + conId).val("").focus();
                }
            }
        });

    //Handle Send button(s) click
    $(document.body).on("click",
        "input[id^='Send-']",
        function () {
            var conId = $(this).data("connection-id");

            if ($("#Message-" + conId).val() !== "") {
                // Call the Send method on the hub.
                chat.server.sendChatMessage({
                    Message: $("#Message-" + conId).val(),
                    Name: $("#DisplayName").val(),
                    Group: conId,
                    ConnectionId: conId,
                    RepliedByBot: true,
                    AttendedByAgent: true,
                    ProfilePictureLocation: $("#ProfilePictureLocation").val()
                });
                // Clear text box and reset focus for next comment.
                $("#Message-" + conId).val("").focus();
            }
        });

    //Handles tabs close icon click
    $(document).on("click",
        ".closeTab",
        function () {
            const disconnectConfirm = confirm("¿Estás seguro de cerrar esta sesión de chat?");

            if (disconnectConfirm) {
                //there are multiple elements which has .closeTab icon so close the tab whose close icon is clicked
                var tabContentId = $(this).parent().attr("href");
                $(this).parent().parent().remove(); //remove li of tab
                $('#tabs a:last').tab('show'); // Select first tab
                $(tabContentId).remove(); //remove respective tab content
            }
        });

    //When user closes the chat window, disconnect from hub
    window.onbeforeunload = function () {
        $.connection.hub.stop();
        adminHub.server.getAllUsers();
    }

    $(document).on("click",
        "#logoutAgent",
        function () {
            const disconnectConfirm = confirm("¿Estás seguro de cerrar sesión?");

            if (disconnectConfirm) {
                adminHub.server.getAllUsers();
                $.connection.hub.stop();
            }
        });

    $.connection.hub.logging = true;
    // Start the connection.
    $.connection.hub.start().done(function () {
        notifHub.server.connectUserAgent();
        adminHub.server.getAllUsers();
        notifHub.server.getNotifications();

        $(".chat_list[data-request-id]").each(function (index, item) {

            const notificationDate = new Date($(item).data("request-date-time"));
            let alertColor = "";
            let messageAlert = "";

            if (((new Date) - notificationDate) < oneMinute) {
                messageAlert = "Nuevo mensaje en espera.";
                alertColor = "active_chat";
                notifyMessage("Necesito de tu ayuda, el usuario te está esperando para ser atendido.");
            } else if (((new Date) - notificationDate) > oneMinute &&
                ((new Date) - notificationDate) < fiveMinutes) {
                alertColor = "warning-chat";
                messageAlert = "Mensaje sin respuesta.";
                notifyMessage("Hay usuarios en espera, por favor atiendelos.");
            } else if (((new Date) - notificationDate) > fiveMinutes) {
                alertColor = "danger-chat";
                messageAlert = "Mensaje sin respuesta, esta petición desaparecerá en 1 minuto.";
                setTimeout(function () {
                    notifHub.client.getNotifications({
                        Name: $("#DisplayName").val(),
                        Group: connId,
                        ConnectionId: connId,
                        RepliedByBot: true,
                        AttendedByAgent: true
                    },
                        $(this).data("request-id"));
                }, 86400000);
            }

            $(item).removeClass("active_chat warning-chat danger-chat").addClass(alertColor);
        });
    });
});

function notifyMessage(user) {
    var text = user;
    if (!document.hasFocus()) {
        if ("Notification" in window) {
            if (Notification.permission === "granted") {
                activateAlertSami();
                this.sendDesktopNotification(text);
                //alert(text);

            } else {
                activateAlertSami();
                this.sendDesktopNotification(text);
                //alert(text);
            }
        } else {
            activateAlertSami();
            this.sendDesktopNotification(text);
            //alert(text);
        }
    }
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

//Helper functionos
function htmlEncode(val) {
    return $("<div />").text(val).html();
}

function htmlDecode(html) {
    const txt = document.createElement("textarea");
    txt.innerHTML = html;
    return txt.value;
}

