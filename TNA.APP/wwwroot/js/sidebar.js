document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("sidebar")
    const mainWrapper = document.getElementById("mainWrapper")
    const sidebarToggle = document.getElementById("sidebarToggle")
    const mobileSidebarToggle = document.getElementById("mobileSidebarToggle")
    const sidebarOverlay = document.getElementById("sidebarOverlay")

    function isMobile() {
        return window.innerWidth <= 992
    }

    function loadSidebarState() {
        if (!isMobile()) {
            const isCollapsed = localStorage.getItem("sidebarCollapsed") === "true"
            if (isCollapsed) {
                collapseSidebar()
            }
        }
    }

    function saveSidebarState(isCollapsed) {
        localStorage.setItem("sidebarCollapsed", isCollapsed)
    }

    function collapseSidebar() {
        sidebar.classList.add("collapsed")
        mainWrapper.classList.add("sidebar-collapsed")
        saveSidebarState(true)
    }

    function expandSidebar() {
        sidebar.classList.remove("collapsed")
        mainWrapper.classList.remove("sidebar-collapsed")
        saveSidebarState(false)
    }

    function toggleSidebar() {
        if (sidebar.classList.contains("collapsed")) {
            expandSidebar()
        } else {
            collapseSidebar()
        }
    }

    function openMobileSidebar() {
        sidebar.classList.add("mobile-open")
        sidebarOverlay.classList.add("active")
        document.body.style.overflow = "hidden"
    }

    function closeMobileSidebar() {
        sidebar.classList.remove("mobile-open")
        sidebarOverlay.classList.remove("active")
        document.body.style.overflow = ""
    }

    function toggleMobileSidebar() {
        if (sidebar.classList.contains("mobile-open")) {
            closeMobileSidebar()
        } else {
            openMobileSidebar()
        }
    }

    function handleResize() {
        if (isMobile()) {
            sidebar.classList.remove("collapsed")
            mainWrapper.classList.remove("sidebar-collapsed")
            closeMobileSidebar()
        } else {
            closeMobileSidebar()
            loadSidebarState()
        }
    }

    if (sidebarToggle) {
        sidebarToggle.addEventListener("click", toggleSidebar)
    }

    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener("click", toggleMobileSidebar)
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener("click", closeMobileSidebar)
    }

    const navLinks = document.querySelectorAll(".sidebar-nav-link")
    navLinks.forEach((link) => {
        link.addEventListener("click", () => {
            if (isMobile()) {
                closeMobileSidebar()
            }
        })
    })

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && isMobile() && sidebar.classList.contains("mobile-open")) {
            closeMobileSidebar()
        }
    })

    window.addEventListener("resize", handleResize)

    loadSidebarState()

    function setActiveNavLink() {
        const currentPath = window.location.pathname.toLowerCase()
        const navLinks = document.querySelectorAll(".sidebar-nav-link")

        navLinks.forEach((link) => {
            link.classList.remove("active")
            const href = link.getAttribute("href")
            if (href && currentPath.includes(href.toLowerCase())) {
                link.classList.add("active")
            }
        })
    }

    setActiveNavLink()
})
