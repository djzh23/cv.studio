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
    }
};

// Backward compatibility for older calls.
window.resumeVersioner = window.CvStudio;
