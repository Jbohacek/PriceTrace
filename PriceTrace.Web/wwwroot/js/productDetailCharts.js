// Chart.js initialization for Product Detail page

window.productDetailCharts = {
    priceChart: null,
    timelineChart: null,

    initializePriceDistribution: function (listings) {
        const canvas = document.getElementById('priceChartCanvas');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');

        // Filter valid prices
        const validListings = listings.filter(l => l.price > 0);
        if (validListings.length === 0) return;

        // Create price ranges
        const prices = validListings.map(l => l.price).sort((a, b) => a - b);
        const minPrice = Math.floor(Math.min(...prices) / 100) * 100;
        const maxPrice = Math.ceil(Math.max(...prices) / 100) * 100;
        const range = maxPrice - minPrice;
        const bucketSize = Math.max(100, Math.ceil(range / 10 / 100) * 100);

        const buckets = {};
        for (let i = minPrice; i <= maxPrice; i += bucketSize) {
            buckets[i] = 0;
        }

        validListings.forEach(listing => {
            const bucket = Math.floor(listing.price / bucketSize) * bucketSize;
            buckets[bucket] = (buckets[bucket] || 0) + 1;
        });

        const labels = Object.keys(buckets).map(k => `${k}-${parseInt(k) + bucketSize} Kè`);
        const data = Object.values(buckets);

        if (this.priceChart) {
            this.priceChart.destroy();
        }

        this.priceChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Number of Listings',
                    data: data,
                    backgroundColor: 'rgba(54, 162, 235, 0.6)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    title: {
                        display: true,
                        text: 'Price Distribution'
                    }
                }
            }
        });
    },

    initializeTimeline: function (listings) {
        const canvas = document.getElementById('timelineChartCanvas');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');

        // Filter valid prices and sort by date
        const validListings = listings
            .filter(l => l.price > 0)
            .sort((a, b) => new Date(a.retrievedAt) - new Date(b.retrievedAt));

        if (validListings.length === 0) return;

        const labels = validListings.map(l => new Date(l.retrievedAt).toLocaleDateString());
        const data = validListings.map(l => l.price);

        if (this.timelineChart) {
            this.timelineChart.destroy();
        }

        this.timelineChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Price (Kè)',
                    data: data,
                    borderColor: 'rgba(75, 192, 192, 1)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: false
                    }
                },
                plugins: {
                    legend: {
                        display: true
                    },
                    title: {
                        display: true,
                        text: 'Price Over Time'
                    }
                }
            }
        });
    },

    destroy: function () {
        if (this.priceChart) {
            this.priceChart.destroy();
            this.priceChart = null;
        }
        if (this.timelineChart) {
            this.timelineChart.destroy();
            this.timelineChart = null;
        }
    }
};
