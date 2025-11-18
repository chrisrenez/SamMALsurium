/**
 * HeroCarousel - A reusable, content-agnostic carousel component
 *
 * Features:
 * - Auto-advance with configurable interval (default: 10 seconds)
 * - Manual navigation (arrow buttons, indicator dots)
 * - Touch/swipe support for mobile devices
 * - Smooth slide animations with directional control
 * - Timer reset on user interaction
 * - Infinite looping (wraps from last to first slide)
 * - Configurable pause-on-hover
 * - Full accessibility support (ARIA labels, keyboard navigation ready)
 *
 * Usage:
 * 1. Add carousel HTML markup with the following structure:
 *    - Container with ID/selector
 *    - Slides with class 'carousel-slide'
 *    - Arrow buttons with classes 'carousel-arrow-left' and 'carousel-arrow-right'
 *    - Indicator dots container with class 'carousel-indicators'
 *    - Individual dots with class 'carousel-dot'
 *
 * 2. Initialize the carousel:
 * @example
 * ```typescript
 * import HeroCarousel from '/js/HeroCarousel.js';
 *
 * const carousel = new HeroCarousel('#hero-carousel', {
 *   interval: 10000,        // Auto-advance every 10 seconds
 *   transitionSpeed: 500,   // 500ms slide transition
 *   pauseOnHover: false     // Don't pause on hover
 * });
 *
 * carousel.init();
 * ```
 *
 * @example
 * ```typescript
 * // With custom configuration
 * const carousel = new HeroCarousel('#my-carousel', {
 *   interval: 5000,         // Faster auto-advance (5 seconds)
 *   transitionSpeed: 300,   // Faster transitions
 *   pauseOnHover: true      // Pause when user hovers
 * });
 *
 * carousel.init();
 * ```
 */
/**
 * HeroCarousel class - Encapsulates all carousel functionality
 */
export class HeroCarousel {
    /**
     * Creates a new HeroCarousel instance
     * @param selector - CSS selector for the carousel container element
     * @param config - Configuration options
     */
    constructor(selector, config = {}) {
        this.currentIndex = 0;
        this.timerId = null;
        this.isAnimating = false;
        // Cached DOM elements
        this.slidesContainer = null;
        this.leftArrow = null;
        this.rightArrow = null;
        this.indicatorsContainer = null;
        this.indicatorDots = [];
        // Touch/swipe tracking
        this.touchStartX = 0;
        this.touchEndX = 0;
        this.touchStartY = 0;
        this.touchEndY = 0;
        this.swipeThreshold = 50; // Minimum distance for swipe in pixels
        // Find container element
        const element = document.querySelector(selector);
        if (!element || !(element instanceof HTMLElement)) {
            throw new Error(`HeroCarousel: Element not found for selector "${selector}"`);
        }
        this.container = element;
        // Merge config with defaults
        this.config = {
            interval: config.interval ?? 10000,
            transitionSpeed: config.transitionSpeed ?? 500,
            pauseOnHover: config.pauseOnHover ?? false
        };
        // Initialize slides array (will be populated in initialization)
        this.slides = [];
    }
    /**
     * Initializes the carousel
     * This method should be called after the DOM is ready
     */
    init() {
        // Cache DOM elements
        this.cacheDOMElements();
        // Verify we have slides
        if (this.slides.length < 2) {
            console.warn('HeroCarousel: At least 2 slides are required');
            return;
        }
        if (this.slides.length > 10) {
            console.warn('HeroCarousel: Maximum 10 slides recommended');
        }
        // Set CSS custom property for animation speed
        this.container.style.setProperty('--carousel-speed', `${this.config.transitionSpeed}ms`);
        console.log(`HeroCarousel initialized with ${this.slides.length} slides`);
        // Verify cached elements (will be used in Tasks 5 and 7)
        if (!this.slidesContainer || !this.leftArrow || !this.rightArrow || !this.indicatorsContainer) {
            console.warn('HeroCarousel: Some DOM elements are missing');
        }
        // Set up event listeners for navigation controls
        this.setupArrowButtons();
        this.setupIndicatorDots();
        // Set up touch/swipe support for mobile
        this.setupTouchSupport();
        // Set up pause-on-hover functionality if configured
        this.setupPauseOnHover();
        // Start auto-advance timer
        this.startTimer();
        // Temporarily expose for debugging
        window.__carouselDebug = {
            next: () => this.nextSlide(),
            prev: () => this.previousSlide(),
            reset: () => this.resetTimer()
        };
    }
    /**
     * Caches all DOM elements needed for carousel functionality
     */
    cacheDOMElements() {
        // Cache slides container
        this.slidesContainer = this.container.querySelector('.carousel-slides');
        // Cache slides
        this.slides = Array.from(this.container.querySelectorAll('.carousel-slide'));
        // Cache arrow buttons
        this.leftArrow = this.container.querySelector('.carousel-arrow-left');
        this.rightArrow = this.container.querySelector('.carousel-arrow-right');
        // Cache indicator dots container and dots
        this.indicatorsContainer = this.container.querySelector('.carousel-indicators');
        this.indicatorDots = Array.from(this.container.querySelectorAll('.carousel-dot'));
        // Find current active slide index
        const activeSlide = this.slides.findIndex(slide => slide.classList.contains('active'));
        if (activeSlide !== -1) {
            this.currentIndex = activeSlide;
        }
    }
    /**
     * Navigates to a specific slide by index
     * @param index - The index of the slide to navigate to
     * @param direction - Optional direction for animation ('forward' or 'reverse')
     */
    goToSlide(index, direction) {
        // Bounds checking with infinite loop logic
        if (index < 0) {
            index = this.slides.length - 1; // Wrap to last slide
        }
        else if (index >= this.slides.length) {
            index = 0; // Wrap to first slide
        }
        // Don't do anything if we're already on this slide
        if (index === this.currentIndex) {
            return;
        }
        // Prevent multiple simultaneous animations
        if (this.isAnimating) {
            return;
        }
        // Determine animation direction if not specified
        if (!direction) {
            direction = index > this.currentIndex ? 'forward' : 'reverse';
        }
        // Set animation flag
        this.isAnimating = true;
        const previousSlide = this.slides[this.currentIndex];
        const nextSlide = this.slides[index];
        // Update current index
        this.currentIndex = index;
        // Perform slide animation
        this.animateSlides(previousSlide, nextSlide, direction);
        // Update indicator dots
        this.updateIndicators();
        console.log(`Navigating to slide ${this.currentIndex} (${direction})`);
    }
    /**
     * Animates the transition between two slides
     * @param currentSlide - The slide that is currently visible
     * @param nextSlide - The slide to transition to
     * @param direction - Animation direction ('forward' or 'reverse')
     */
    animateSlides(currentSlide, nextSlide, direction) {
        // Remove any existing animation classes
        this.cleanupAnimationClasses(currentSlide);
        this.cleanupAnimationClasses(nextSlide);
        // Determine animation classes based on direction
        let exitClass;
        let enterClass;
        if (direction === 'forward') {
            // Forward: current exits left, next enters from right
            exitClass = 'exit-left';
            enterClass = 'enter-right';
        }
        else {
            // Reverse: current exits right, next enters from left
            exitClass = 'exit-right';
            enterClass = 'enter-left';
        }
        // Make next slide visible and position it for animation
        nextSlide.style.visibility = 'visible';
        nextSlide.style.opacity = '1';
        // Add animation classes
        currentSlide.classList.add(exitClass);
        nextSlide.classList.add(enterClass);
        // Wait for animation to complete
        setTimeout(() => {
            // Remove active class from old slide
            currentSlide.classList.remove('active');
            currentSlide.style.visibility = 'hidden';
            currentSlide.style.opacity = '0';
            // Add active class to new slide
            nextSlide.classList.add('active');
            // Clean up animation classes
            this.cleanupAnimationClasses(currentSlide);
            this.cleanupAnimationClasses(nextSlide);
            // Reset animation flag
            this.isAnimating = false;
        }, this.config.transitionSpeed);
    }
    /**
     * Removes all animation classes from a slide
     * @param slide - The slide element to clean up
     */
    cleanupAnimationClasses(slide) {
        slide.classList.remove('exit-left', 'exit-right', 'enter-left', 'enter-right');
    }
    /**
     * Navigates to the next slide
     */
    nextSlide() {
        this.goToSlide(this.currentIndex + 1, 'forward');
    }
    /**
     * Navigates to the previous slide
     */
    previousSlide() {
        this.goToSlide(this.currentIndex - 1, 'reverse');
    }
    /**
     * Updates the active state of indicator dots
     */
    updateIndicators() {
        this.indicatorDots.forEach((dot, index) => {
            if (index === this.currentIndex) {
                dot.classList.add('active');
                dot.setAttribute('aria-current', 'true');
            }
            else {
                dot.classList.remove('active');
                dot.removeAttribute('aria-current');
            }
        });
    }
    /**
     * Destroys the carousel and cleans up event listeners
     */
    destroy() {
        this.stopTimer();
        // TODO: Remove event listeners
    }
    /**
     * Starts the auto-advance timer
     */
    startTimer() {
        // Stop any existing timer first
        this.stopTimer();
        // Start new timer for auto-advance
        this.timerId = window.setInterval(() => {
            this.nextSlide();
        }, this.config.interval);
        console.log(`Auto-advance timer started (${this.config.interval}ms interval)`);
    }
    /**
     * Stops the auto-advance timer
     */
    stopTimer() {
        if (this.timerId !== null) {
            window.clearInterval(this.timerId);
            this.timerId = null;
            console.log('Auto-advance timer stopped');
        }
    }
    /**
     * Resets the auto-advance timer
     * Stops the current timer and starts a new one
     */
    resetTimer() {
        console.log('Auto-advance timer reset');
        this.stopTimer();
        this.startTimer();
    }
    /**
     * Sets up click event listeners for arrow buttons
     */
    setupArrowButtons() {
        // Left arrow - navigate to previous slide
        if (this.leftArrow) {
            this.leftArrow.addEventListener('click', () => {
                console.log('Left arrow clicked');
                this.previousSlide();
                this.resetTimer();
            });
        }
        else {
            console.warn('HeroCarousel: Left arrow button not found');
        }
        // Right arrow - navigate to next slide
        if (this.rightArrow) {
            this.rightArrow.addEventListener('click', () => {
                console.log('Right arrow clicked');
                this.nextSlide();
                this.resetTimer();
            });
        }
        else {
            console.warn('HeroCarousel: Right arrow button not found');
        }
    }
    /**
     * Sets up click event listeners for indicator dots
     */
    setupIndicatorDots() {
        if (this.indicatorDots.length === 0) {
            console.warn('HeroCarousel: No indicator dots found');
            return;
        }
        // Attach click listener to each dot
        this.indicatorDots.forEach((dot, index) => {
            dot.addEventListener('click', () => {
                console.log(`Indicator dot ${index} clicked`);
                // Determine direction based on target index vs current index
                const direction = index > this.currentIndex ? 'forward' : 'reverse';
                // Navigate to the clicked slide
                this.goToSlide(index, direction);
                // Reset the timer
                this.resetTimer();
            });
        });
        console.log(`Set up ${this.indicatorDots.length} indicator dots`);
    }
    /**
     * Sets up touch event listeners for swipe gestures on mobile
     */
    setupTouchSupport() {
        // Touch start - record initial position
        this.container.addEventListener('touchstart', (e) => {
            this.touchStartX = e.touches[0].clientX;
            this.touchStartY = e.touches[0].clientY;
        }, { passive: true });
        // Touch move - record current position
        this.container.addEventListener('touchmove', (e) => {
            this.touchEndX = e.touches[0].clientX;
            this.touchEndY = e.touches[0].clientY;
        }, { passive: true });
        // Touch end - detect swipe and navigate
        this.container.addEventListener('touchend', () => {
            this.handleSwipe();
        });
        console.log('Touch support enabled');
    }
    /**
     * Handles swipe gesture detection and navigation
     */
    handleSwipe() {
        const deltaX = this.touchEndX - this.touchStartX;
        const deltaY = this.touchEndY - this.touchStartY;
        // Calculate absolute distances
        const absX = Math.abs(deltaX);
        const absY = Math.abs(deltaY);
        // Only process horizontal swipes (where horizontal movement > vertical movement)
        if (absX < this.swipeThreshold) {
            // Swipe distance too small, ignore
            return;
        }
        if (absY > absX) {
            // Vertical swipe (scroll), don't interfere
            return;
        }
        // Determine swipe direction and navigate
        if (deltaX > 0) {
            // Swipe right - go to previous slide
            console.log('Swipe right detected');
            this.previousSlide();
            this.resetTimer();
        }
        else {
            // Swipe left - go to next slide
            console.log('Swipe left detected');
            this.nextSlide();
            this.resetTimer();
        }
        // Reset touch coordinates
        this.touchStartX = 0;
        this.touchEndX = 0;
        this.touchStartY = 0;
        this.touchEndY = 0;
    }
    /**
     * Sets up pause-on-hover functionality if configured
     */
    setupPauseOnHover() {
        if (!this.config.pauseOnHover) {
            return;
        }
        // Pause timer when mouse enters carousel
        this.container.addEventListener('mouseenter', () => {
            console.log('Pausing auto-advance (hover)');
            this.stopTimer();
        });
        // Resume timer when mouse leaves carousel
        this.container.addEventListener('mouseleave', () => {
            console.log('Resuming auto-advance (hover end)');
            this.startTimer();
        });
    }
}
export default HeroCarousel;
//# sourceMappingURL=HeroCarousel.js.map