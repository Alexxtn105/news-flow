window.themeManager = {
    get: function () {
        return localStorage.getItem('nf-theme') || 'dark';
    },
    set: function (mode) {
        localStorage.setItem('nf-theme', mode);
        document.documentElement.setAttribute('data-theme', mode);
    }
};
