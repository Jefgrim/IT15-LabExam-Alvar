// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
	const revealElements = Array.from(document.querySelectorAll(".reveal-up"));
	if (!revealElements.length) {
		return;
	}

	revealElements.forEach(function (element, index) {
		element.style.transitionDelay = (index * 50) + "ms";
	});

	if ("IntersectionObserver" in window) {
		const observer = new IntersectionObserver(function (entries) {
			entries.forEach(function (entry) {
				if (entry.isIntersecting) {
					entry.target.classList.add("is-visible");
					observer.unobserve(entry.target);
				}
			});
		}, { threshold: 0.16 });

		revealElements.forEach(function (element) {
			observer.observe(element);
		});

		return;
	}

	revealElements.forEach(function (element) {
		element.classList.add("is-visible");
	});
})();
