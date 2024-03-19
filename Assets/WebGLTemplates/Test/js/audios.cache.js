window.AudiosCache = {
    LogAudios: [0],
    LogAudio: function (blob, duration, rec) {
        var set = rec && rec.set || {};
        var id = this.LogAudio.length;
        this.LogAudios.push({ blob: blob, set: $.extend({}, set), duration: duration });
    },
    LogClear: function () {
        this.LogAudios = [0];
    }
};