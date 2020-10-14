$(function () {
    cargarListaCompanias(1);
});

// Función para listar los usuarios.
function cargarListaCompanias(page) {
    // Lee los parametros para ejecutar la acci贸n requerida
    $("#carga2").fadeIn('slow');
    $.ajax({
        beforeSend: function (objeto) {
            $('.listado_companias').html('');
            $("#carga2").html('<center><img src="https://3.bp.blogspot.com/-T_2Mk0VWsPs/WKh_DNP_02I/AAAAAAAABF4/oBTlwNI52u8mdo9Y5deIxBzg7Em4n2pvQCLcB/s400/loading%2Bgif%2B1.gif" class="img-responsive center-block"/></center>');
        },
        url: ListCompanies.ValueUrl,
        success: function (data) {
            $('.listado_companias').html(data);
            $('#carga2').html("");
        },
        error: function (data) {
            console.log(data);
        }
    });
}

/**
 * <summary>
 *      @description: Permite visualizar la imagen adjuntada.
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} imageUploader
 * @param {any} previewImage
 */

function ShowImagePreview(imageUploader, previewImage) {
    if (imageUploader.files && imageUploader.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            $(previewImage).attr('src', e.target.result);
        };
        reader.readAsDataURL(imageUploader.files[0]);
    }
}

var fileToRead = document.getElementById("file1");

var banderaTamano = false;

fileToRead.addEventListener("change", function (event) {

    var foto = fileToRead.files[0];
    var c = 0;

    if (fileToRead.files.length === 0 || !(/\.(jpg|png)$/i).test(foto.name)) {
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
/**
 * <summary>
 *      @description: Lleva los datos del form al controlador
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} form
 */
function jQueryAjaxPost(form) {
    $.validator.unobtrusive.parse(form);
    if ($(form).valid()) {
        var ajaxConfig = {
            type: 'POST',
            url: '@Url.Action("Create", "CompanyAdmin")',
            data: new FormData(form),
            contentType: false,
            processData: false,
            success: function (response) {
                console.log(response);
                if (response.success) {
                    $('#allListC').html(response.html);
                    refreshAddNewTab($(form).attr('data-restUrl'), true);
                    alertify.success(response.message);
                    if (typeof activatejQueryTable !== 'undefined' && $.isFunction(activatejQueryTable))
                        activatejQueryTable();
                } else {
                    alertify.error(response.message);
                }
            }
        }

        if ($(form).attr('enctype') === "multipart/form-data") {
            ajaxConfig["contentType"] = false;
            ajaxConfig["processData"] = false;
        }

        $.ajax(ajaxConfig);
    }

    return false;
}
/**
 /**
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
            $("#addCompanieTab").html(response);
            $('ul.nav.nav-tabs a:eq(1)').html('Agregar Nueva Compañía');
            if (showViewTab) {
                $('ul.nav.nav-tabs a:eq(0)').tab('show');
            }
        }
    })
}
/**
/**
 * <summary>
 *      @description: Al darle clic en editar usuario, se elimina la pestaña de Agregar nuevo usuario y se reemplaza por Editar Usuario.
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} url
 */
function EditC(url) {
    $.ajax({
        type: 'GET',
        url: url,
        success: function (response) {
            $("#addCompanieTab").html(response);
            $('ul.nav.nav-tabs a:eq(1)').html('Editar Compañía');
            $('ul.nav.nav-tabs a:eq(1)').tab('show');
        }
    })
}

/**
 * <summary>
 *      @description: Se refresca la lista al momento de eliminar un usuario.
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} url
 */
function DeleteGet(url) {
    $.ajax({
        type: 'GET',
        url: url,
        success: function (response) {
            $("#addCompanieTab").html(response);
            $('ul.nav.nav-tabs a:eq(1)').html('Eliminar Compañía');
            $('ul.nav.nav-tabs a:eq(1)').tab('show');
        }
    })
}
/**
 * <summary>
 *      @description: Eliminar un usuario.
 *      @author: Juan David Parroquiano Vargas - WorkSpace
 * </summary>
 * @param {any} url
 */

function DeleteC(url) {
    console.log(url);
    alertify.confirm("Alerta de Confirmación", "¿Desea eliminar esta compañía?", function () {
        $.ajax({
            type: 'POST',
            url: url,
            success: function (response) {
                console.log(response);
                if (response.success) {
                    $("#allListC").html(response.html);
                    alertify.success(response.message);
                    if (typeof activatejQueryTable !== 'undefined' && $.isFunction(activatejQueryTable))
                        activatejQueryTable();
                    $("#allListC").load(location.href + " #allListC>*", "");
                }
                else {
                    alertify.error(response.message);
                }
            }
        });
    }, function () {

    }).set('labels', { ok: 'Eliminar Compañía', cancel: 'Cancelar' });
}