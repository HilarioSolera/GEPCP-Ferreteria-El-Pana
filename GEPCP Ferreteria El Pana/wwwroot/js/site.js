// ═══════════════════════════════════════════════════════════════
// GEPCP FERRETERÍA EL PANA - Sistema de Loading y Notificaciones
// ═══════════════════════════════════════════════════════════════

// ───────────────────────────────────────────────────────────────
// LOADING GLOBAL NO BLOQUEANTE
// ───────────────────────────────────────────────────────────────

const Loading = {
    indicator: null,
    text: null,
    activeCount: 0,

    init() {
        if (!this.indicator) {
            this.indicator = document.getElementById('loading-indicator');
            this.text = document.getElementById('loading-indicator-text');
        }
    },

    show(message = 'Procesando...') {
        this.init();
        this.activeCount++;

        if (this.text && message) {
            this.text.textContent = message;
        }

        if (this.indicator) {
            this.indicator.classList.add('show');
            this.indicator.setAttribute('aria-hidden', 'false');
        }
    },

    hide(force = false) {
        this.init();

        if (force) {
            this.activeCount = 0;
        } else if (this.activeCount > 0) {
            this.activeCount--;
        }

        if (this.activeCount === 0 && this.indicator) {
            this.indicator.classList.remove('show');
            this.indicator.setAttribute('aria-hidden', 'true');
        }
    }
};

// ───────────────────────────────────────────────────────────────
// TOAST NOTIFICATIONS - Notificaciones visuales
// ───────────────────────────────────────────────────────────────

const Toast = {
    container: null,

    init() {
        if (!this.container) {
            this.container = document.getElementById('toast-container');
            if (!this.container) {
                this.container = document.createElement('div');
                this.container.id = 'toast-container';
                this.container.style.cssText = 'position: fixed; bottom: 20px; right: 20px; z-index: 9999;';
                document.body.appendChild(this.container);
            }
        }
    },

    show(message, type = 'info', duration = 3000) {
        this.init();
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        toast.style.cssText = `
            padding: 12px 20px;
            margin-bottom: 10px;
            border-radius: 4px;
            color: white;
            font-weight: 500;
            animation: slideIn 0.3s ease-out;
        `;

        const bgColor = type === 'error' ? '#dc3545' : type === 'success' ? '#28a745' : '#17a2b8';
        toast.style.backgroundColor = bgColor;

        this.container.appendChild(toast);

        if (duration > 0) {
            setTimeout(() => {
                toast.style.animation = 'slideOut 0.3s ease-out';
                setTimeout(() => toast.remove(), 300);
            }, duration);
        }
    },

    success(message) { this.show(message, 'success'); },
    error(message) { this.show(message, 'error'); },
    info(message) { this.show(message, 'info'); }
};

// ───────────────────────────────────────────────────────────────
// FORM LOADING - Deshabilitar botones durante envío
// ───────────────────────────────────────────────────────────────

function setupFormLoading(formSelector) {
    const form = document.querySelector(formSelector);
    if (!form) return;

    form.addEventListener('submit', function (e) {
        const submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Procesando...';
            submitBtn.style.opacity = '0.6';
        }
    });
}

function setButtonLoading(buttonSelector, loading = true) {
    const button = document.querySelector(buttonSelector);
    if (!button) return;

    if (loading) {
        button.dataset.originalText = button.textContent;
        button.textContent = 'Cargando...';
        button.disabled = true;
    } else {
        button.textContent = button.dataset.originalText || 'Enviar';
        button.disabled = false;
    }
}

function submitFormWithLoading(formSelector, endpoint, method = 'POST') {
    const form = document.querySelector(formSelector);
    if (!form) return;

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        const submitBtn = form.querySelector('button[type="submit"]');

        try {
            Loading.show('Procesando...');
            if (submitBtn) submitBtn.disabled = true;

            const formData = new FormData(form);
            const response = await fetch(endpoint, {
                method: method,
                body: formData
            });

            if (response.ok) {
                Toast.success('Datos guardados correctamente');
                setTimeout(() => form.submit(), 500);
            } else {
                Toast.error('Error al guardar los datos');
            }
        } catch (error) {
            Toast.error('Error en la solicitud: ' + error.message);
        } finally {
            Loading.hide();
            if (submitBtn) submitBtn.disabled = false;
        }
    });
}

function handleDownloadWithLoading(url, filename) {
    Loading.show('Descargando...');

    fetch(url)
        .then(response => response.blob())
        .then(blob => {
            const link = document.createElement('a');
            link.href = window.URL.createObjectURL(blob);
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(link.href);
            Toast.success('Archivo descargado correctamente');
        })
        .catch(error => {
            Toast.error('Error al descargar el archivo');
            console.error('Error:', error);
        })
        .finally(() => {
            Loading.hide();
        });
}

function initializeGlobalLoading() {
    const submitSelectors = [
        'form.form-loading',
        'form[data-loading-message]'
    ];

    submitSelectors.forEach(selector => {
        document.querySelectorAll(selector).forEach(form => {
            if (form.dataset.loadingBound === '1') return;
            form.dataset.loadingBound = '1';

            form.addEventListener('submit', function (e) {
                if (form.dataset.loadingSubmitting === '1') {
                    return;
                }

                const message =
                    form.getAttribute('data-loading-message') ||
                    'Procesando...';

                Loading.show(message);

                const submitButton = document.activeElement &&
                    document.activeElement.closest('button[type="submit"],input[type="submit"]')
                    ? document.activeElement.closest('button[type="submit"],input[type="submit"]')
                    : form.querySelector('button[type="submit"],input[type="submit"]');

                if (submitButton) {
                    submitButton.disabled = true;
                    submitButton.classList.add('disabled');
                }

                form.dataset.loadingSubmitting = '1';
            });
        });
    });

    const longActionSelectors = [
        'a[data-loading-message]',
        'a.long-action',
        'a[href*="DescargarPDF"]',
        'a[href*="DescargarBoleta"]',
        'a[href*="DescargarFiniquito"]',
        'a[href*="ExportarPDF"]',
        'a[href*="ExportarExcel"]'
    ];

    longActionSelectors.forEach(selector => {
        document.querySelectorAll(selector).forEach(link => {
            if (link.dataset.loadingBound === '1') return;
            link.dataset.loadingBound = '1';

            link.addEventListener('click', function (e) {
                const message =
                    link.getAttribute('data-loading-message') ||
                    'Procesando descarga...';

                Loading.show(message);

                const href = link.getAttribute('href');
                if (!href || href === '#') {
                    return;
                }

                e.preventDefault();

                setTimeout(function () {
                    const target = link.getAttribute('target');
                    if (target && target !== '_self') {
                        window.open(href, target);
                    } else {
                        window.location.href = href;
                    }
                }, 60);

                setTimeout(() => Loading.hide(), 8000);
            });
        });
    });

    const hideLoading = () => Loading.hide(true);

    window.addEventListener('pageshow', hideLoading);
    window.addEventListener('focus', hideLoading);
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) hideLoading();
    });
}

// ───────────────────────────────────────────────────────────────
// EXPORTAR FUNCIONES GLOBALES
// ───────────────────────────────────────────────────────────────

window.Loading = Loading;
window.Toast = Toast;
window.setupFormLoading = setupFormLoading;
window.setButtonLoading = setButtonLoading;
window.submitFormWithLoading = submitFormWithLoading;
window.handleDownloadWithLoading = handleDownloadWithLoading;
window.initializeGlobalLoading = initializeGlobalLoading;

// ═══════════════════════════════════════════════════════════════
// MONITOREO DE CONEXIÓN - SIN CIERRE AUTOMÁTICO
// ═══════════════════════════════════════════════════════════════

(function () {
    initializeGlobalLoading();

    const pingInterval = setInterval(function () {
        try {
            navigator.sendBeacon('/api/server/ping', JSON.stringify({}));
        } catch (e) {
        }
    }, 3000);

    window.addEventListener('unload', function () {
        if (pingInterval) {
            clearInterval(pingInterval);
        }
    });
})();
