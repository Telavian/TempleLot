function checkDocumentLoaded() {
    return document.readyState !== 'loading';
}

function initializePageFrame(isObjectionsHidden) {
    console.log("Initializing page");
    addCSSItemsStyle(isObjectionsHidden);
    addVerseFinder();
}

function addCSSItemsStyle(isObjectionsHidden) {
    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;
    var styleNode = frameWindowDocument.getElementById('umStyleElement');
    if (styleNode) {
        styleNode.remove();
    }

    var color = "#A8C6EF";

    var objectionStyle = isObjectionsHidden
        ? "mono { display: none; }"
        : "";

    var style = `.tl-search-item { background: ${color} !important; } ${objectionStyle}`;

    var element = frameWindowDocument.createElement("style");
    element.setAttribute("id", "umStyleElement");
    element.innerText = style;
    frameWindowDocument.head.appendChild(element);
}

function addVerseFinder() {
    try {
        var frameWindow = document.getElementById("pageFrame").contentWindow;
        var frameWindowDocument = frameWindow.document;
        var container = frameWindowDocument.getElementById("page-container");

        if (frameWindowDocument.getElementById("snackbar") != null) {
            return;
        }

        var snackbar = frameWindowDocument.createElement("div");
        snackbar.id = "snackbar";
        container.appendChild(snackbar);

        container.addEventListener('click', (event) => {
            var [book1, distance1] = findClosest(event.target, "book");
            var [book2, distance2] = findClosest(event.target, "sectiontitle");
            var [book3, distance3] = findClosest(event.target, "sectionnumber");

            var book = null;
            var isDC = false;
            var isTitle = false;

            if (distance1 < distance2 && distance1 < distance3 && getNodeText(book1).length > 0) {
                book = book1;
            }

            if (distance2 <= distance1 && distance2 <= distance3 && getNodeText(book2).length > 0) {
                book = book2;
                isDC = true;
                isTitle = true;
            }

            if (distance3 <= distance1 && distance3 <= distance2 && getNodeText(book3).length > 0) {
                book = book3;
                isDC = true;
                isTitle = false;
            }

            var fullName = "";

            if (isDC) {
                var [verse, verseDistance] = findClosest(event.target, "verse");

                if (isTitle) {
                    fullName = `${toProperCase(getNodeText(book))}:${getNodeText(verse)}`;
                }
                else {
                    fullName = `Section ${toProperCase(getNodeText(book))}:${getNodeText(verse)}`;
                }
            }
            else {
                var [chapter, chapterDistance] = findClosest(event.target, "chapter");
                var [verse, verseDistance] = findClosest(event.target, "verse");

                fullName = `${toProperCase(getNodeText(book))} ${getNodeText(chapter)}:${getNodeText(verse)}`;
            }

            fullName = fullName.replace(/:+$/, '').trimEnd();

            if (fullName.length == 0) {
                return;
            }

            var selection = frameWindow.getSelection();

            if (selection != null && selection.type == "Range") {
                return;
            }

            var snackbar = frameWindowDocument.getElementById("snackbar");
            snackbar.innerText = fullName;
            snackbar.className = "show";

            setTimeout(function () { snackbar.className = snackbar.className.replace("show", ""); }, 3000);
        });
    }
    catch (ex) {
        console.log(ex.message);
    }
}

function findClosest(element, name) {
    var distance = 0;

    while (true) {
        if (element == null) {
            break;
        }

        if (element.previousElementSibling != null) {
            element = element.previousElementSibling;
            distance++;

            var isMatch = element.nodeName.toLowerCase() == name.toLowerCase();

            if (isMatch) {
                return [element, distance];
            }

            var foundNode = element.querySelector(name);

            if (foundNode != null) {
                return [foundNode, distance];
            }

            continue;
        }

        distance++;
        element = element.parentElement;
    }

    return [null, distance];
}

function getNodeText(node) {
    if (node == null) {
        return "";
    }

    if (node.innerText == null) {
        return "";
    }

    return node.innerText;
}

function toProperCase(text) {
    let upper = true;
    let newStr = "";

    for (let i = 0, l = text.length; i < l; i++) {
        if (text[i] == " ") {
            upper = true;
            newStr += text[i];
            continue;
        }

        newStr += upper ? text[i].toUpperCase() : text[i].toLowerCase();
        upper = false;
    }

    return newStr;
}

function setCurrentFrameScrollLocation(location) {
    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;
    var container = frameWindowDocument.getElementById("page-container");

    container.scrollTop = location;
    return true;
}

function getCurrentFrameScrollLocation() {
    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;
    var container = frameWindowDocument.getElementById("page-container");

    // Some platforms return double (android)
    return Math.trunc(container.scrollTop);
}

function removeCurrentSearchMatches() {
    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;
    let items = frameWindowDocument.querySelectorAll('.tl-search-item');

    items.forEach(function (item) {
        item.classList.remove('tl-search-item');
    });
}

function highlightSearchResults(json, isHighlight) {
    try {
        var elementPaths = JSON.parse(json);
        removeCurrentSearchMatches();

        return highlightSearchMatches(elementPaths, isHighlight);
    } catch (e) { console.log(e); }

    return false;
}

function highlightSearchMatches(elementPaths, isHighlight) {
    var isScrolled = false;
    var isComplete = true;

    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;

    elementPaths.forEach(xpath => {
        if (isComplete == false) {
            return false;
        }

        var result = frameWindowDocument
            .evaluate(xpath, frameWindowDocument, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);

        var selectionElement = result.singleNodeValue;
        if (selectionElement == null) {
            isComplete = false;
            return false;
        }

        result = frameWindowDocument
            .evaluate(".//*[@id='page-container']", frameWindowDocument, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);

        var containerElement = result.singleNodeValue;
        if (containerElement == null) {
            isComplete = false;
            return false;
        }

        if (isScrolled === false) {
            scrollToElement(selectionElement, 4);
            isScrolled = true;
        }

        if (selectionElement.classList.contains("tl-search-item")) {
            return;
        }

        if (isHighlight) {
            selectionElement.classList.add("tl-search-item");
        }
    });

    return isComplete && isScrolled;
}

function scrollToElement(element, count) {
    if (element == null) {
        return;
    }

    if (count <= 0) {
        element.scrollIntoView(true);
        return;
    }

    /* scrollIntoView sometimes does not always directly work */
    var node = element;
    for (var x = 0; x < count; x++) {
        if (node.previousSibling != null && node.previousElementSibling != null) {
            node = node.previousElementSibling;
        }
        else if (node.parentElement != null) {
            node = node.parentElement;
        }
    }

    node.scrollIntoView(true);
    return scrollToElement(element, count - 1);
}

function findPosition(node) {
    var top = 0;
    var isExact = true;

    if (node.offsetTop === 0) {
        isExact = false;
    }

    while (node != null) {
        top += (node.offsetTop || 0);
        node = node.parentNode;
    }

    return [top, isExact];
}

function getSelectedNodes() {
    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;
    var selection = frameWindowDocument.getSelection();

    if (selection == null || selection.isCollapsed) {
        return;
    }

    var start = selection.anchorNode;
    var end = selection.focusNode;

    if (start == null || end == null) {
        return;
    }

    var position = start.compareDocumentPosition(end);
    if (position & Node.DOCUMENT_POSITION_PRECEDING) {
        [start, end] = [end, start];
    }

    var nodes = new Set();

    while (start != end) {
        if (start.nodeType == 3) {
            nodes.add(start.parentNode);
        }
        else {
            nodes.add(start);
        }

        if (start.firstChild) {
            start = start.firstChild;
        }
        else if (start.nextSibling) {
            start = start.nextSibling;
        }
        else if (start.parentNode.nextSibling) {
            start = start.parentNode.nextSibling;
        } else if (start.parentNode.parentNode.nextSibling) {
            start = start.parentNode.parentNode.nextSibling;
        }
        else if (start.parentNode.parentNode.parentNode.nextSibling) {
            start = start.parentNode.parentNode.parentNode.nextSibling;
        }
        else if (start.parentNode.parentNode.parentNode.parentNode.nextSibling) {
            start = start.parentNode.parentNode.parentNode.parentNode.nextSibling;
        }
        else if (start.parentNode.parentNode.parentNode.parentNode.parentNode.nextSibling) {
            start = start.parentNode.parentNode.parentNode.parentNode.parentNode.nextSibling;
        }
    }

    if (end.nodeType == 3) { /* Text */
        nodes.add(end.parentNode);
    }
    else {
        nodes.add(end);
    }

    var results = Array.from(nodes);
    return results;
}

function getSelectedText() {
    var nodes = getSelectedNodes();
    var text = "";

    if (nodes == null || nodes.length == 0) {
        return "";
    }

    nodes.forEach(node => {
        text += node.innerText;
    });

    return text;
}

function getSelectedXPaths() {
    var nodes = getSelectedNodes();
    var xpath = [];

    if (nodes == null || nodes.length == 0) {
        return [];
    }

    nodes.forEach(node => {
        xpath.push(getPathTo(node));
    });

    return xpath;
}

function getPathTo(element) {
    if (element.id !== '')
        return `id('${element.id}')`;
    if (element === document.body)
        return element.tagName;

    var ix = 0;
    var siblings = element.parentNode.childNodes;

    for (var i = 0; i < siblings.length; i++) {
        var sibling = siblings[i];
        if (sibling === element)
            return `${getPathTo(element.parentNode)}/${element.tagName}[${(ix + 1)}]`;
        if (sibling.nodeType === 1 && sibling.tagName === element.tagName)
            ix++;
    }
}

function updateHighlightColor(xpath, color) {
    var frameWindow = document.getElementById("pageFrame").contentWindow;
    var frameWindowDocument = frameWindow.document;

    var result = frameWindowDocument
        .evaluate(xpath, frameWindowDocument, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);

    if (result == null || result.singleNodeValue == null) {
        alert("Not found");
        return;
    }

    result.singleNodeValue.style = `background-color: ${color};`;

    if ((color === null || color.trim() === "") && result.singleNodeValue.classList.contains("tl-search-item")) {
        removeCurrentSearchMatches();
    }
}