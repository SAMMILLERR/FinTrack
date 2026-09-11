window.FinTrack = {

    toggleTheme: function () {

        document.body.classList.toggle("dark-mode");

        const isDark =
            document.body.classList.contains("dark-mode");

        localStorage.setItem(
            "theme",
            isDark ? "dark" : "light"
        );
    },

    loadTheme: function () {

        const theme =
            localStorage.getItem("theme");

        if (theme === "dark") {

            document.body.classList.add("dark-mode");

        } else {

            document.body.classList.remove("dark-mode");

        }
    },

    auth: {

        login: async function (email, password) {

            const response = await fetch(
                "/api/auth/login",
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        email: email,
                        password: password
                    })
                }
            );

            if (response.ok) {

                const result =
                    await response.json();

                window.location.href =
                    result.redirectUrl;

                return;
            }

            if (response.status === 401) {

                alert("Invalid email or password.");
                return;
            }

            if (response.status === 429) {

                alert(
                    "Too many login attempts. Please wait one minute."
                );

                return;
            }

            if (response.status === 400) {

                const result =
                    await response.json();

                alert(
                    result.message ||
                    "Your account has been deactivated."
                );

                return;
            }

            alert("Unable to login right now.");
        },

        logout: async function () {

            try {

                const response =
                    await fetch(
                        "/api/auth/logout",
                        {
                            method: "POST"
                        }
                    );

                window.location.href =
                    "/account/login";

            }
            catch (error) {

                console.error(
                    "Logout failed:",
                    error
                );

                window.location.href =
                    "/account/login";
            }
        }
    },

    downloadFile: function (
        fileName,
        contentType,
        base64Data
    ) {

        try {

            const byteCharacters =
                atob(base64Data);

            const byteNumbers =
                new Array(byteCharacters.length);

            for (
                let i = 0;
                i < byteCharacters.length;
                i++
            ) {

                byteNumbers[i] =
                    byteCharacters.charCodeAt(i);
            }

            const byteArray =
                new Uint8Array(byteNumbers);

            const blob =
                new Blob(
                    [byteArray],
                    { type: contentType }
                );

            const url =
                window.URL.createObjectURL(blob);

            const link =
                document.createElement("a");

            link.href = url;
            link.download = fileName;
            link.style.display = "none";

            document.body.appendChild(link);

            link.click();

            document.body.removeChild(link);

            setTimeout(
                function () {
                    window.URL.revokeObjectURL(url);
                },
                1000
            );

        }
        catch (error) {

            console.error(
                "CSV download failed:",
                error
            );

            alert(
                "Unable to download the CSV file."
            );
        }
    }
};