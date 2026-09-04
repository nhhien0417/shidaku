mergeInto(LibraryManager.library, {

    ParseDesignFromFileUpload: function (separator) {
            console.log("ParseDesignFromFileUpload");
            
            var fileuploader = document.getElementById("fileuploader");
            if (!fileuploader) {
                fileuploader = document.createElement("input");
                fileuploader.setAttribute("style","display:none;");
                fileuploader.setAttribute("type", "file");
                fileuploader.setAttribute("class", "native-file-input");
                fileuploader.setAttribute("id", "fileuploader");
                document.getElementsByTagName("body")[0].appendChild(fileuploader);
                
                fileuploader.onchange = async function(e) {
                    let files = e.target.files;
                    let data = []
                    
                    try {
                        for (var i = 0, f; f = files[i]; i++) {
                            let res = await fetch(URL.createObjectURL(f))
                            let text = await res.text()
                            data.push(parseDesignData(text, f.name))
                        }
                        
                        SendMessage('WebGLMessageHandler', 'HandleData', data.join(separator));
                    } catch (e) {
                        alert(e)
                        SendMessage('WebGLMessageHandler', 'HandleData', "");
                    }
                }
              
                fileuploader.oncancel = function(e) {
                    SendMessage('WebGLMessageHandler', 'HandleData', "");
                }
            }
            fileuploader.click();
    },

    ParseDesignFromDrawIOData: function (data) {
        console.log("ParseDesignFromDrawIOData");
    
        let contentStr = UTF8ToString(data);
        let designStr = parseDesignData(contentStr, "0");
        
        let bufferSize = lengthBytesUTF8(designStr) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(designStr, buffer, bufferSize);
        return buffer;
    }
});