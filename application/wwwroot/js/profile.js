function openProfileModal() {

    const modal = document.getElementById("profileModal");

    if (modal) {
        modal.classList.add("active");
    }

}



function closeProfileModal() {

    const modal = document.getElementById("profileModal");

    if (modal) {
        modal.classList.remove("active");
    }

}

function openConfirmModal() {

    const modal =
        document.getElementById(
            "confirmModal"
        );


    modal.classList.add("active");

}



function closeConfirmModal() {

    const modal =
        document.getElementById(
            "confirmModal"
        );


    modal.classList.remove("active");

}



function submitProfileUpdate() {

    document
        .getElementById(
            "updateProfileForm"
        )
        .submit();

}

function openPasswordModal() {

    const modal =
        document.getElementById("passwordModal");


    if (modal) {

        modal.classList.add("active");

    }

}



function closePasswordModal() {

    const modal =
        document.getElementById("passwordModal");


    if (modal) {

        modal.classList.remove("active");

    }

}



function openPasswordConfirmModal() {

    const modal =
        document.getElementById("passwordConfirmModal");


    if (modal) {

        modal.classList.add("active");

    }

}

function closePasswordConfirmModal() {

    const modal =
        document.getElementById("passwordConfirmModal");


    if (modal) {

        modal.classList.remove("active");

    }

}

function submitPasswordUpdate() {

    document
        .getElementById("passwordForm")
        .submit();

}

function togglePassword(inputId, icon) {

    const input =
        document.getElementById(inputId);


    if (input.type === "password") {

        input.type = "text";

        icon.textContent = "🙈";

    }
    else {

        input.type = "password";

        icon.textContent = "👁";

    }

}

window.onclick = function (event) {

    const modal =
        document.getElementById("profileModal");


    if (event.target === modal) {

        modal.classList.remove("active");

    }

}





document.addEventListener(
    "DOMContentLoaded",
    function () {

        const success =
            document.getElementById(
                "successMessage"
            );


        const error =
            document.getElementById(
                "errorMessage"
            );



        if (success) {

            setTimeout(function () {

                success.style.opacity = "0";

                success.style.transform =
                    "translateY(-20px)";


                setTimeout(function () {

                    success.remove();

                }, 500);


            }, 3000);

        }





        if (error) {

            setTimeout(function () {

                error.style.opacity = "0";

                error.style.transform =
                    "translateY(-20px)";


                setTimeout(function () {

                    error.remove();

                }, 500);


            }, 3000);

        }



    });