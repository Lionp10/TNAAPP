// Sidebar functionality for TNA ESPORT Dashboard
document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("sidebar")
    const mainWrapper = document.getElementById("mainWrapper")
    const sidebarToggle = document.getElementById("sidebarToggle")
    const mobileSidebarToggle = document.getElementById("mobileSidebarToggle")
    const sidebarOverlay = document.getElementById("sidebarOverlay")

    // Check if we're on mobile
    function isMobile() {
        return window.innerWidth <= 992
    }

    // Load saved sidebar state from localStorage
    function loadSidebarState() {
        if (!isMobile()) {
            const isCollapsed = localStorage.getItem("sidebarCollapsed") === "true"
            if (isCollapsed) {
                collapseSidebar()
            }
        }
    }

    // Save sidebar state to localStorage
    function saveSidebarState(isCollapsed) {
        localStorage.setItem("sidebarCollapsed", isCollapsed)
    }

    // Collapse sidebar (desktop)
    function collapseSidebar() {
        sidebar.classList.add("collapsed")
        mainWrapper.classList.add("sidebar-collapsed")
        saveSidebarState(true)
    }

    // Expand sidebar (desktop)
    function expandSidebar() {
        sidebar.classList.remove("collapsed")
        mainWrapper.classList.remove("sidebar-collapsed")
        saveSidebarState(false)
    }

    // Toggle sidebar (desktop)
    function toggleSidebar() {
        if (sidebar.classList.contains("collapsed")) {
            expandSidebar()
        } else {
            collapseSidebar()
        }
    }

    // Open mobile sidebar
    function openMobileSidebar() {
        sidebar.classList.add("mobile-open")
        sidebarOverlay.classList.add("active")
        document.body.style.overflow = "hidden"
    }

    // Close mobile sidebar
    function closeMobileSidebar() {
        sidebar.classList.remove("mobile-open")
        sidebarOverlay.classList.remove("active")
        document.body.style.overflow = ""
    }

    // Toggle mobile sidebar (open/close)
    function toggleMobileSidebar() {
        if (sidebar.classList.contains("mobile-open")) {
            closeMobileSidebar()
        } else {
            openMobileSidebar()
        }
    }

    // Handle window resize
    function handleResize() {
        if (isMobile()) {
            // Mobile: reset desktop states
            sidebar.classList.remove("collapsed")
            mainWrapper.classList.remove("sidebar-collapsed")
            closeMobileSidebar()
        } else {
            // Desktop: close mobile sidebar and load saved state
            closeMobileSidebar()
            loadSidebarState()
        }
    }

    // Event listeners
    if (sidebarToggle) {
        sidebarToggle.addEventListener("click", toggleSidebar)
    }

    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener("click", toggleMobileSidebar)
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener("click", closeMobileSidebar)
    }

    // Close mobile sidebar when clicking on nav links
    const navLinks = document.querySelectorAll(".sidebar-nav-link")
    navLinks.forEach((link) => {
        link.addEventListener("click", () => {
            if (isMobile()) {
                closeMobileSidebar()
            }
        })
    })

    // Handle escape key
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && isMobile() && sidebar.classList.contains("mobile-open")) {
            closeMobileSidebar()
        }
    })

    // Handle window resize
    window.addEventListener("resize", handleResize)

    // Initialize sidebar state
    loadSidebarState()

    // Set active nav link based on current URL
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

    // Set active link on page load
    setActiveNavLink()
})
