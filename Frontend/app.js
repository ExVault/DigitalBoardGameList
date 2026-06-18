tippy.setDefaultProps({
    theme: "light-border",
    allowHTML: true,
    animation: "scale",
    placement: "top",
});

const NOW = new Date();

const PLATFORM_NAMES = [
    "GooglePlay",
    "AppStore",
    "Steam",
    "GOG",
    "EGS",
    "Playstation",
    "Xbox",
    "Switch",
];

const PLATFORM_ICONS_PATH = "./assets/platform_icons/";

const PLATFORM_ICONS = initPlatformIcons(PLATFORM_NAMES);

function initPlatformIcons(names, ext = ".svg") {
    return Object.fromEntries(names.map(n => [n, `${PLATFORM_ICONS_PATH}${n.toLowerCase()}${ext}`]));
}

function getIconForPlatform(platform) {
    return PLATFORM_ICONS[platform] ?? PLATFORM_ICONS_PATH + "no_icon.svg";
}

let tableData = [];
let sortState = {col: null, dir: 1};

const SORT_KEYS = {
    0: null, // Image
    1: game => game.Name,
    2: game => game.Bgg.Rank,
    3: game => game.Urls.length,
    4: game => game.Prices.length === 0 ? Infinity : game.Prices[0].Price.Value,
    5: game => game.LastUpdates.length === 0 ? "0000-00-00" : game.LastUpdates[0].LastUpdate,
    6: game => game.Dlcs ? game.Dlcs.length : 0,
    7: game => game.Developer ?? "",
};

function initSortHandlers() {
    document.querySelectorAll("#gamesTable thead th").forEach((th, i) => {

        if (SORT_KEYS[i] === null) return;

        th.style.cursor = "pointer";
        th.style.userSelect = "none";
        //th.dataset.sortArrow = "↕";
        th.dataset.sortArrow = "";

        th.addEventListener("click", () => {
            if (sortState.col === i) {
                sortState.dir *= -1;
            } else {
                sortState.col = i;
                // Links, Last Update, DLCs - sort descending
                if (i === 3 || i === 5 || i === 6) {
                    sortState.dir = -1;
                } else {
                    sortState.dir = 1;
                }
            }
            updateSortIndicators();
            const sorted = [...tableData].sort((a, b) => {
                const ka = SORT_KEYS[i](a);
                const kb = SORT_KEYS[i](b);
                if (ka < kb) return -1 * sortState.dir;
                if (ka > kb) return sortState.dir;
                return 0;
            });
            renderTable(sorted);
        });
    });
}

function updateSortIndicators() {
    document.querySelectorAll("#gamesTable thead th").forEach((th, i) => {
        if (SORT_KEYS[i] === null) return;
        if (sortState.col === i) {
            th.dataset.sortArrow = sortState.dir === 1 ? "↑" : "↓";
        } else {
            th.dataset.sortArrow = ""
            //th.dataset.sortArrow = "↕";
        }
    });
}

initSortHandlers();

fetch("data.json")
    .then(res => res.json())
    .then(data => {
        tableData = data.Games;
        renderTable(data.Games);
        addFooter(data)
    })
    .catch(err => {
        console.error("Failed to load data.json:", err);
        const colCount = document.querySelector("#gamesTable thead tr").cells.length;
        document.querySelector("#gamesTable tbody").innerHTML =
            `<tr><td colspan="${colCount}">Failed to load data. Please try again later.</td></tr>`;
    });

let tippyInstances = [];

function renderTable(gameData) {
    tippyInstances.forEach(instance => instance.destroy());
    tippyInstances = [];

    const tbody = document.querySelector("#gamesTable tbody");
    tbody.innerHTML = "";

    const docFrag = document.createDocumentFragment();

    for (const game of gameData) {
        const tr = document.createElement("tr");

        tr.appendChild(makeImageTd(game));
        tr.appendChild(makeNameTd(game));
        tr.appendChild(makeRankTd(game));
        tr.appendChild(makePlatformLinksTd(game));
        tr.appendChild(makePriceTd(game));
        tr.appendChild(makeLastUpdateTd(game));
        tr.appendChild(makeDlcsTd(game));
        tr.appendChild(makeDeveloperTd(game));

        docFrag.appendChild(tr);
    }

    tbody.appendChild(docFrag);
}

function makeImageTd(game) {
    const td = document.createElement("td");
    if (game.ImageUrl) {
        const img = document.createElement("img");
        img.src = game.ImageUrl;
        img.className = "game-image";
        td.appendChild(img);
    }
    return td;
}

function makeNameTd(game) {
    const td = document.createElement("td");
    const a = document.createElement("a");
    a.href = game.Bgg.Url;
    a.textContent = game.Name;
    a.target = "_blank";
    td.appendChild(a);
    return td;
}

function makeRankTd(game) {
    const td = document.createElement("td");
    td.textContent = game.Bgg.Rank;
    return td;
}

function makePlatformLinksTd(game) {
    const td = document.createElement("td");
    td.className = "platforms";

    for (const {Platform, Url} of game.Urls) {
        const wrapper = document.createElement("span");
        wrapper.className = "platform-item";

        const a = document.createElement("a");
        a.href = Url;
        a.target = "_blank";
        a.setAttribute("aria-label", `View on ${Platform}`);
;
        const img = document.createElement("img");
        img.src = getIconForPlatform(Platform);
        img.alt = Platform;
        a.appendChild(img);

        wrapper.appendChild(a);
        td.appendChild(wrapper);
    }

    return td;
}

function makePriceTd(game) {
    const priceTd = document.createElement("td");
    
    const prices = game.Prices;

    if (prices.length === 0) return priceTd;

    let max = prices[prices.length - 1].Price.Value;

    if (max === 0) {
        priceTd.textContent = "Free to play";
        return priceTd;
    }
    
    if (prices.length === 1) {
        priceTd.appendChild(document.createTextNode('$' + max));
    }
    else {
        let min = prices[0].Price.Value;
        let minStr = stringifyZero(min);
        const span = document.createElement("span");
        span.className = "hover-hint";
        span.textContent = min === max ? `${minStr}` : `${minStr} - $${max}`;

        const tippyInstance = tippy(span, {
            content: prices
                .map(({Platform, Price}) => makeTippyRow(Platform, stringifyZero(Price.Value)))
                .join("")
        });
        tippyInstances.push(tippyInstance);

        priceTd.appendChild(span);
    }
    
    appendPlatformSaleBadges(priceTd, game);
    return priceTd;
}

function stringifyZero(value) {
    return value === 0 ? "Free to play" : '$' + value;
}

function makeTippyRow(platform, value){
    return `<div class="tooltip-price-row"><img src="${getIconForPlatform(platform)}" class="tooltip-platform-icon" alt="${platform}" /> ${value}</div>`;
}

function appendPlatformSaleBadges(td, game) {
    const discounted = game.Prices.filter(p => p.Price.DiscountPct > 0);
    if (discounted.length === 0) return;

    td.appendChild(document.createElement("br"));

    const container = document.createElement("div");
    container.className = "sale-badges-container";

    for (const {Platform, Price} of discounted) {
        const badge = document.createElement("span");
        badge.className = "sale-badge";

        const img = document.createElement("img");
        img.src = getIconForPlatform(Platform);
        img.alt = Platform;
        img.className = "sale-badge-icon";
        badge.appendChild(img);

        const textNode = document.createTextNode(`-${Price.DiscountPct}%`);
        badge.appendChild(textNode);

        container.appendChild(badge);
    }
    td.appendChild(container);
}

function makeLastUpdateTd(game) {
    const td = document.createElement("td");

    let lastUpdates = game.LastUpdates;

    if (lastUpdates.length === 0) return td;

    const min = lastUpdates[0].LastUpdate;

    const span = document.createElement("span");
    span.classList.add("last-update", `${freshnessClass(min)}`);
    span.textContent = elapsedTimeAsString(min);

    if (lastUpdates.length === 1) {
        td.appendChild(span);
        return td;
    }

    span.classList.add("hover-hint");

    const tippyInstance = tippy(span, {
        content: lastUpdates
            .map(({Platform, LastUpdate}) => makeTippyRow(Platform, elapsedTimeAsString(LastUpdate)))
            .join(""),
    });
    tippyInstances.push(tippyInstance);

    td.appendChild(span);
    return td;
}

function dateDiffDays(now, then) {
    return Math.floor((now - then) / (1000 * 60 * 60 * 24));
}

function freshnessClass(date) {
    const diffDays = dateDiffDays(NOW, new Date(date));

    if (diffDays < 180) return "freshness-green";
    if (diffDays < 365) return "freshness-yellow";
    if (diffDays < 365 * 3) return "freshness-orange";
    return "freshness-red";
}

function elapsedTimeAsString(date) {
    const diffDays = dateDiffDays(NOW, new Date(date));
    if (diffDays === 0) return "Today";
    if (diffDays === 1) return "1 day ago";
    if (diffDays <= 30) return `${diffDays} days ago`;
    
    const diffMonths = Math.floor(diffDays / 30.44);
    if (diffMonths === 1) return "1 month ago";
    if (diffMonths < 12) return `${diffMonths} months ago`;
    
    const diffYears = Math.floor(diffDays / 365.25);
    return diffYears === 1 ? "1 year ago" : `${diffYears} years ago`;
}

function makeDlcsTd(game) {

    const td = document.createElement("td");

    const dlcs = game.Dlcs;

    if (!dlcs || dlcs.length === 0) return td;

    if (dlcs.length === 1) {
        td.textContent = dlcs[0];
        return td;
    }

    const span = document.createElement("span");
    span.className = "hover-hint";
    span.textContent = `${dlcs[0]}...+${dlcs.length - 1}`;

    const tippyInstance = tippy(span, {
        content: dlcs.map(e => `■ ${e}`).join("<br>"),
    });
    tippyInstances.push(tippyInstance);

    td.appendChild(span);

    return td;
}

function makeDeveloperTd(game) {

    const td = document.createElement("td");

    const dev = game.Developer;

    if (!dev) return td;

    const pub = game.Publisher;

    if (!pub || dev.toLowerCase() === pub.toLowerCase()) {
        td.textContent = dev;
        return td;
    }

    const br = document.createElement("br");
    td.appendChild(document.createTextNode(dev));
    td.appendChild(br);
    td.appendChild(document.createTextNode(pub));

    return td;
}

function addFooter(data){
    const footer = document.getElementById("footer");
    const publishDate = new Date(data.PublishDate);
    const formatted = publishDate.toLocaleString("en-GB", { year: "numeric", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit", hour12: false });
    footer.textContent = `Total games: ${data.Games.length} · Last update: ${formatted}`;
}
