window.CvStudio = {
    downloadFile: function (fileName, base64Content, contentType) {
        const link = document.createElement('a');
        link.download = fileName;
        link.href = `data:${contentType};base64,${base64Content}`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },
    setLastResumeId: function (resumeId) {
        window.localStorage.setItem("resumeVersioner.lastResumeId", resumeId);
    },
    getLastResumeId: function () {
        return window.localStorage.getItem("resumeVersioner.lastResumeId");
    },
    clearLastResumeId: function () {
        window.localStorage.removeItem("resumeVersioner.lastResumeId");
    },
    notify: function (message) {
        window.alert(message);
    },
    setAccessGranted: function (isGranted) {
        if (isGranted) {
            window.sessionStorage.setItem("cvstudio.access.granted", "1");
            return;
        }

        window.sessionStorage.removeItem("cvstudio.access.granted");
    },
    getAccessGranted: function () {
        return window.sessionStorage.getItem("cvstudio.access.granted") === "1";
    },
    clearAccessGranted: function () {
        window.sessionStorage.removeItem("cvstudio.access.granted");
    }
};

window.resumeVersioner = window.CvStudio;
