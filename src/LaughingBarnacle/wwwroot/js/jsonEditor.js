/**
 * Monaco Editor interop for the Blazor JSON editor component.
 * The Monaco loader is included via CDN in App.razor.
 */
window.jsonEditorInterop = (function () {
    let editor = null;

    return {
        /**
         * Initialize Monaco Editor inside the given element.
         * @param {string} elementId  - The id of the container div.
         * @param {string} content    - Initial JSON content to display.
         */
        initialize: function (elementId, content) {
            require.config({
                paths: { vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.0/min/vs' }
            });

            require(['vs/editor/editor.main'], function () {
                const element = document.getElementById(elementId);
                if (!element) return;

                // Dispose previous instance (navigation back/forward).
                if (editor) {
                    editor.dispose();
                    editor = null;
                }

                editor = monaco.editor.create(element, {
                    value: content,
                    language: 'json',
                    theme: 'vs-dark',
                    automaticLayout: true,
                    folding: true,
                    foldingStrategy: 'indentation',
                    showFoldingControls: 'always',
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    fontSize: 14,
                    tabSize: 2,
                    formatOnPaste: true,
                    formatOnType: true,
                    wordWrap: 'off'
                });
            });
        },

        /**
         * Returns the current editor content.
         * @returns {string}
         */
        getContent: function () {
            return editor ? editor.getValue() : '';
        },

        /**
         * Triggers a browser download of the current editor content.
         * @param {string} filename - Suggested file name for the download.
         */
        downloadContent: function (filename) {
            const content = editor ? editor.getValue() : '';
            const blob = new Blob([content], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = filename;
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            URL.revokeObjectURL(url);
        },

        /**
         * Disposes the Monaco Editor instance.
         */
        dispose: function () {
            if (editor) {
                editor.dispose();
                editor = null;
            }
        }
    };
}());
