window.webauthn = {
    register: async function (options) {
        try {
            // Decode standard base64 strings to Uint8Array
            if (options.challenge) {
                options.challenge = coerceToArrayBuffer(options.challenge);
            }
            if (options.user && options.user.id) {
                options.user.id = coerceToArrayBuffer(options.user.id);
            }
            if (options.excludeCredentials) {
                options.excludeCredentials = options.excludeCredentials.map((c) => {
                    c.id = coerceToArrayBuffer(c.id);
                    return c;
                });
            }

            const cred = await navigator.credentials.create({
                publicKey: options
            });

            return JSON.stringify({
                id: cred.id,
                rawId: coerceToBase64Url(cred.rawId),
                type: cred.type,
                extensions: cred.getClientExtensionResults(),
                response: {
                    attestationObject: coerceToBase64Url(cred.response.attestationObject),
                    clientDataJSON: coerceToBase64Url(cred.response.clientDataJSON),
                    transports: cred.response.getTransports ? cred.response.getTransports() : []
                }
            });
        } catch (e) {
            throw e;
        }
    },

    login: async function (optionsJson) {
        try {
            const options = typeof optionsJson === 'string' ? JSON.parse(optionsJson) : optionsJson;

            if (options.challenge) {
                options.challenge = coerceToArrayBuffer(options.challenge);
            }
            if (options.allowCredentials) {
                options.allowCredentials = options.allowCredentials.map((c) => {
                    c.id = coerceToArrayBuffer(c.id);
                    return c;
                });
            }

            const cred = await navigator.credentials.get({
                publicKey: options
            });

            return JSON.stringify({
                id: cred.id,
                rawId: coerceToBase64Url(cred.rawId),
                type: cred.type,
                extensions: cred.getClientExtensionResults(),
                response: {
                    authenticatorData: coerceToBase64Url(cred.response.authenticatorData),
                    clientDataJSON: coerceToBase64Url(cred.response.clientDataJSON),
                    signature: coerceToBase64Url(cred.response.signature),
                    userHandle: cred.response.userHandle ? coerceToBase64Url(cred.response.userHandle) : null
                }
            });
        } catch (e) {
            throw e;
        }
    }
};

function coerceToArrayBuffer(thing) {
    if (typeof thing === "string") {
        thing = thing.replace(/-/g, "+").replace(/_/g, "/");

        var str = window.atob(thing);
        var bytes = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++) {
            bytes[i] = str.charCodeAt(i);
        }
        return bytes;
    }

    if (Array.isArray(thing)) {
        return new Uint8Array(thing);
    }

    return thing;
}

function coerceToBase64Url(thing) {
    if (Array.isArray(thing)) {
        thing = Uint8Array.from(thing);
    }

    if (thing instanceof ArrayBuffer) {
        thing = new Uint8Array(thing);
    }

    if (thing instanceof Uint8Array) {
        var str = "";
        var len = thing.byteLength;

        for (var i = 0; i < len; i++) {
            str += String.fromCharCode(thing[i]);
        }
        thing = window.btoa(str);
    }

    if (typeof thing !== "string") {
        throw new Error("could not coerce to string");
    }

    thing = thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

    return thing;
}