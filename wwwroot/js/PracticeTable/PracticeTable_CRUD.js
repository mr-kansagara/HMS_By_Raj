var Details = function (id) {
    var url = "/PracticeTable/Details?id=" + id;
    $('#titleExtraBigModal').html("Practice Table data");
    loadExtraBigModal(url);
};


function validateImageFile(input) {
    const file = input.files[0];
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif'];

    if (file && allowedTypes.includes(file.type)) {
        // Valid image file selected
        var reader = new FileReader();

        reader.onload = function (e) {
            var img = document.getElementById('imagePreview');
            img.src = e.target.result;
            img.style.display = 'block'; // Show the image preview
        };

        reader.readAsDataURL(file);
        document.getElementById('profilePictureError').textContent = '';
    } else {
        if (input.files.length > 0) {
            // Upload the new image file
            formData.append("ProfilePictureDetails", input.files[0]);
        }
        // Invalid file type selected
        document.getElementById('profilePictureError').textContent = 'Invalid file type. Please select a JPEG, PNG, or GIF image.';
        input.value = ''; // Clear the file input to allow selecting another file

        if (fileInput.files.length < 0) {
            document.getElementById('profilePicture').src = "/images/TheLegendPv/default-profile-image.png";
        }
    }
}

var AddEdit = function (id) {
    var url = "/PracticeTable/AddEdit?id=" + id;
    if (id !== '') {
        $('#titleExtraBigModal').html("Edit Role");
    }
    else {
        $('#titleExtraBigModal').html("Add Role");
    }
    loadExtraBigModal(url);
};

var Save = function () {
    
    if (!$("#practiceDescription").valid()) {
        return;
    }

    var _frmUserRoles = $("#practiceDescription").serialize();

    var formData = new FormData($("#practiceDescription")[0]);

    // Append serialized form data to FormData object
    formData.append("practiceDescription", _frmUserRoles);
 

    // Check if a new image file is provided
    var fileInput = document.getElementById("ProfilePictureDetails");
    var profilePictureElement = document.getElementById("ProfileImage");


    if (fileInput.files.length > 0) {
        // Upload the new image file
        formData.append("ProfileImage", fileInput.files[0]);
    }


    $("#btnSave").val("Please Wait");
    $('#btnSave').attr('disabled', 'disabled');
    $.ajax({
        type: "POST",
        url: "/PracticeTable/AddEdit",
        data: formData,
        contentType: false,
        processData: false,
        success: function (result) {
            console.log(result);

            Swal.fire({
                title: result,
                icon: "success"
            }).then(function () {
                document.getElementById("btnClose").click();
                $("#btnSave").val("Save");
                $('#btnSave').removeAttr('disabled');
                $('#tblPractice').DataTable().ajax.reload();
            });
        },
        error: function (errormessage) {
            SwalSimpleAlert(errormessage.responseText, "warning");
        }
    });
}

var Delete = function (id) {

    Swal.fire({
        title: 'Do you want to delete this item?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes'
    }).then((result) => {
        if (result.value) {
            $.ajax({
                type: "POST",
                url: "/PracticeTable/Delete?id=" + id,
                success: function (result) {
                    var message = "Role has been deleted successfully. Role ID: " + result.Title
                    Swal.fire({
                        title: message,
                        icon: 'info',
                        onAfterClose: () => {
                            $('#tblPractice').DataTable().ajax.reload();
                        }
                    });
                }
            });
        }
    });
};