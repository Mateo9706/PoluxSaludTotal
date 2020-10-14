$(function () {

    // //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    // Declare proxies to reference the hubs.
    var adminHub = $.connection.SamiAdminCrudHub;  
   
    // Start the connection.    
    adminHub.client.getAllUsers = function (listusers) {

        // Html encode display name and message.
        const vm = JSON.parse(listusers);

        // Empty list users
        $("#listado_usuarios").empty();        

        // Listado de los usuarios encontrados
        vm.forEach(function (listUsersConst) {
            
            // Variables
            var username = listUsersConst.UserName; // Nombre del usuario
            var email = listUsersConst.Email; // Correo del usuario
            var Status = listUsersConst.Status; // Estado del usuario 
            var usernamered = listUsersConst.NickNameRed; // Usuario de red.
            var Id = listUsersConst.UserId;
            // Botones

            // Botón para editar
            var buttonEdit = $('<button data-toggle="tooltip" data-placement="top" title="Editar Usuario" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-edit"></i></button>').click(
                function () {
                    Edit('Edit/' + Id + '');
                });

            // Botón para ver detalles
            var buttonDetails = $('<button data-toggle="tooltip" data-placement="top" title="Ver Detalles" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-info"></i></button>').click(
                function () {
                    Details('Details/' + Id + '');
                });

            // Botón para Eliminar
            var buttonDelete = $('<button data-toggle="tooltip" data-placement="top" title="Eliminar" class="btn btn-sami-success btn-xs btn-sami-view"><i class="fa fa-remove"></i></button>').click(
                function () {
                    Delete('Delete/' + Id + '');
                });

            let statusUser = "", nicknamered = "";

            if (usernamered == "null" || usernamered == null || usernamered == "")
                nicknamered = "<span class='badge badge-danger'>No Registrado</span>";
            else
                nicknamered = usernamered;

            if (Status == "0")
                statusUser = "<span class='badge badge-danger'>Offline</span>";
            else
                statusUser = "<span class='badge badge-success'>Online</span>";

            var listUsersPrint = "<tr><td>" + username + "</td><td>" + email + "</td><td>" + nicknamered + "</td><td>" + statusUser + "</td></tr>";
           

            $("#listado_usuarios").append(Mustache.render(listUsersPrint, listUsersConst));
            $("#listado_usuarios tr:last").append('<td></td>').find("td:last").append(buttonEdit, " ", buttonDetails, " ", buttonDelete);
        });
    };

    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        adminHub.server.getAllUsers();
        adminHub.server.welcome();
    });

    window.onbeforeunload = function () {
        $.connection.hub.stop();
    }

    $(document).on("click",
        "#logoutAgent",
        function () {
            const disconnectConfirm = confirm("¿Estás seguro de cerrar sesión?");

            if (disconnectConfirm) {
                $.connection.hub.stop();
            }
        });

});

// Función para editar al agente

function Edit(id) {
    $.ajax({
        type: 'GET',
        url: id,
        success: function (response) {
            $("#addUserTab").html(response);
            $('ul.nav.nav-tabs a:eq(1)').html('Editar Usuario');
            $('ul.nav.nav-tabs a:eq(1)').tab('show');
        }
    });
}

// Función para ver detalles del agente

function Details(id) {

}

// Función para eliminar al agente

function Delete(id) {
    // //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });

    var adminHub = $.connection.SamiAdminCrudHub;

    alertify.confirm("Alerta de Confirmación", "¿Desea eliminar a este usuario?", function () {
        $.ajax({
            type: 'POST',
            url: id,
            success: function (response) {
                if (response.success) {
                    alertify.success("El usuario se ha eliminado correctamente.");
                    adminHub.server.getAllUsers();
                }
                else {
                    alertify.error(response.message);
                }
            }
        });
    }, function () {

    }).set('labels', { ok: 'Eliminar Usuario', cancel: 'Cancelar' });
}

function ShowImagePreview(imageUploader, previewImage) {
    if (imageUploader.files && imageUploader.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            $(previewImage).attr('src', e.target.result);
        };
        reader.readAsDataURL(imageUploader.files[0]);
    }
}

var fileToRead = document.getElementById("file");

var banderaTamano = false;

fileToRead.addEventListener("change", function (event) {

    var foto = fileToRead.files[0];
    var c = 0;

    if (fileToRead.files.length == 0 || !(/\.(jpg|png)$/i).test(foto.name)) {
        alert('Ingrese una imagen con alguno de los siguientes formatos: .jpeg/.jpg/.png.');
        return false;
    }

    // Si el tamaño de la imagen fue validado
    if (banderaTamano) {
        return true;
    }

    var img = new Image();
    img.onload = function dimension() {
        if (this.width.toFixed(0) < 50 && this.height.toFixed(0) < 50) {
            alert('Las medidas deben ser mayores de 50px horizontal y vertical');
        } else {
            alert('Imagen correcta :)' + fileToRead);
            // El tamaño de la imagen fue validado
            banderaTamano = true;

            ShowImagePreview(fileToRead, document.getElementById('imagePreview'));
        }
    };
    img.src = URL.createObjectURL(foto);

    // Devolvemos false porque falta validar el tamaño de la imagen
    return false;

}, false);

/*
 * <summary>
 *      @description: Lleva los datos del form al controlador
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} form
 */
function jQueryAjaxPost(form) {
    // //Config to avoid AJAX caching on IE
    $.ajaxSetup({ cache: false });
    var adminHub = $.connection.SamiAdminCrudHub;
    $.validator.unobtrusive.parse(form);
    if ($(form).valid()) {
        var ajaxConfig = {
            type: 'POST',
            url: '@Url.Action("Register", "UserAdmin")',
            data: new FormData(form),
            contentType: false,
            processData: false,
            success: function (response) {
                console.log(response);
                if (response.success) {
                    $('#allList').html(response.html);
                    refreshAddNewTab($(form).attr('data-restUrl'), true);
                    alertify.success(response.message);
                    adminHub.server.getAllUsers();
                    if (typeof activatejQueryTable !== 'undefined' && $.isFunction(activatejQueryTable))
                        activatejQueryTable();
                } else {
                    alertify.error(response.message);
                }
            }
        };

        if ($(form).attr('enctype') === "multipart/form-data") {
            ajaxConfig["contentType"] = false;
            ajaxConfig["processData"] = false;
        }

        $.ajax(ajaxConfig);
    }

    return false;
}
/*
 *
 * <summary>
 *      @description: Al momento de agregar un usuario, la pestaña se refresca.
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} form
 */
function refreshAddNewTab(resetUrl, showViewTab) {
    $.ajax({
        type: 'GET',
        url: resetUrl,
        success: function (response) {
            $("#addUserTab").html(response);
            $('ul.nav.nav-tabs a:eq(1)').html('Agregar Nuevo Usuario');
            if (showViewTab) {
                $('ul.nav.nav-tabs a:eq(0)').tab('show');
            }
        }
    });
}