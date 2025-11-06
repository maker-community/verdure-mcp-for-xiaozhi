// Infinite Scroll with Intersection Observer API
// Detects when a sentinel element becomes visible and triggers loading more items

class InfiniteScrollObserver {
    constructor() {
        this.observer = null;
        this.dotNetHelper = null;
        this.sentinelElement = null;
    }

    /**
     * Initialize the observer with a callback to .NET
     * @param {object} dotNetHelper - The .NET object reference
     * @param {string} sentinelId - The ID of the sentinel element to observe
     * @param {string} scrollContainerId - The ID of the scroll container (null for viewport)
     * @param {number} threshold - The percentage of visibility to trigger (0.0 to 1.0)
     */
    initialize(dotNetHelper, sentinelId, scrollContainerId = null, threshold = 0.1) {
        this.dotNetHelper = dotNetHelper;
        this.sentinelElement = document.getElementById(sentinelId);

        if (!this.sentinelElement) {
            console.error(`Sentinel element with ID '${sentinelId}' not found`);
            return false;
        }

        // Get scroll container if specified
        const scrollContainer = scrollContainerId ? document.getElementById(scrollContainerId) : null;

        // Create intersection observer
        this.observer = new IntersectionObserver(
            (entries) => this.handleIntersection(entries),
            {
                root: scrollContainer, // Use scroll container or viewport
                rootMargin: '100px', // Start loading 100px before sentinel is visible
                threshold: threshold
            }
        );

        // Start observing
        this.observer.observe(this.sentinelElement);
        console.log(`Infinite scroll observer initialized with ${scrollContainer ? 'scroll container: ' + scrollContainerId : 'viewport'}`);
        return true;
    }

    /**
     * Handle intersection changes
     * @param {IntersectionObserverEntry[]} entries - The intersection entries
     */
    handleIntersection(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                // Sentinel is visible, trigger load more
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OnScrollReachedEnd')
                        .catch(error => {
                            console.error('Error invoking OnScrollReachedEnd:', error);
                        });
                }
            }
        });
    }

    /**
     * Disconnect and cleanup the observer
     */
    dispose() {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.dotNetHelper = null;
        this.sentinelElement = null;
        console.log('Infinite scroll observer disposed');
    }

    /**
     * Temporarily stop observing (useful when loading)
     */
    pause() {
        if (this.observer && this.sentinelElement) {
            this.observer.unobserve(this.sentinelElement);
        }
    }

    /**
     * Resume observing
     */
    resume() {
        if (this.observer && this.sentinelElement) {
            this.observer.observe(this.sentinelElement);
        }
    }
}

// Global instance
window.infiniteScrollObserver = new InfiniteScrollObserver();

// Export functions for Blazor JSInterop
window.infiniteScroll = {
    initialize: (dotNetHelper, sentinelId, threshold) => {
        return window.infiniteScrollObserver.initialize(dotNetHelper, sentinelId, threshold);
    },
    dispose: () => {
        window.infiniteScrollObserver.dispose();
    },
    pause: () => {
        window.infiniteScrollObserver.pause();
    },
    resume: () => {
        window.infiniteScrollObserver.resume();
    }
};
