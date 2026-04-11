window.monacoInterop = (function () {
    let _editor = null;

    return {
        initialize: function (elementId, initialValue) {
            require.config({
                paths: {
                    vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs'
                }
            });

            require(['vs/editor/editor.main'], function () {
                const container = document.getElementById(elementId);
                if (!container) return;

                // Dispose any previous instance
                if (_editor) {
                    _editor.dispose();
                    _editor = null;
                }

                _editor = monaco.editor.create(container, {
                    value: initialValue || '',
                    language: 'json',
                    theme: 'vs-dark',
                    automaticLayout: true,
                    folding: true,
                    foldingStrategy: 'auto',
                    showFoldingControls: 'always',
                    minimap: { enabled: true },
                    scrollBeyondLastLine: false,
                    fontSize: 14,
                    tabSize: 2,
                    wordWrap: 'off',
                    formatOnPaste: true,
                    formatOnType: false
                });
            });
        },

        getValue: function () {
            if (_editor) {
                return _editor.getValue();
            }
            return '';
        },

        setValue: function (value) {
            if (_editor) {
                _editor.setValue(value);
            }
        },

        dispose: function () {
            if (_editor) {
                _editor.dispose();
                _editor = null;
            }
        },

        downloadFile: function (filename, content) {
            const blob = new Blob([content], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }
    };
})();
