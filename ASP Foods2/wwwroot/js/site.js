// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
  const initNavbarScrollState = () => {
    const navbar = document.querySelector('.navbar-sf');

    if (!navbar) {
      return;
    }

    const toggleScrolledState = () => {
      navbar.classList.toggle('scrolled', window.scrollY > 12);
    };

    toggleScrolledState();
    window.addEventListener('scroll', toggleScrolledState, { passive: true });
  };

  const getBrandInitials = (name) => {
    if (!name) {
      return '?';
    }

    return (
      name
        .trim()
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part.charAt(0).toUpperCase())
        .join('') || '?'
    );
  };

  const initBrandPreview = (scope = document) => {
    const input = scope.querySelector('[data-brand-preview-input]');
    const previewText = scope.querySelector('[data-brand-preview-text]');
    const previewBadge = scope.querySelector('[data-brand-preview-badge]');

    if (!input || !previewText || !previewBadge || input.dataset.previewBound === 'true') {
      return;
    }

    const syncPreview = () => {
      const value = input.value.trim();
      const previewValue = value || 'Нова марка';

      previewText.textContent = previewValue;
      previewBadge.textContent = getBrandInitials(previewValue);
    };

    input.dataset.previewBound = 'true';
    syncPreview();
    input.addEventListener('input', syncPreview);
  };

  const initLegacyBrandsPage = () => {
    const normalizedPath = window.location.pathname.replace(/\/+$/, '').toLowerCase();

    if (!['/brands', '/brands/index'].includes(normalizedPath) || document.querySelector('.brands-page')) {
      return;
    }

    const main = document.querySelector('main[role="main"]');
    const heading = main?.querySelector(':scope > h1');
    const createWrapper = heading?.nextElementSibling;
    const table = createWrapper?.nextElementSibling;

    if (!main || !heading || !createWrapper || !(table instanceof HTMLTableElement)) {
      return;
    }

    const createLink = createWrapper.querySelector('a[href]');
    const rows = Array.from(table.querySelectorAll('tbody tr'))
      .map((row) => {
        const cells = row.querySelectorAll('td');
        const name = cells[0]?.textContent?.trim();
        const links = Array.from(cells[1]?.querySelectorAll('a[href]') ?? []);

        if (!name || links.length === 0) {
          return null;
        }

        return {
          name,
          detailsHref: links.find((link) => link.textContent?.trim().toLowerCase() === 'details')?.getAttribute('href') ?? '#',
          editHref: links.find((link) => link.textContent?.trim().toLowerCase() === 'edit')?.getAttribute('href') ?? '#',
          deleteHref: links.find((link) => link.textContent?.trim().toLowerCase() === 'delete')?.getAttribute('href') ?? '#',
          id: links[0]?.getAttribute('href')?.split('/').pop() ?? ''
        };
      })
      .filter(Boolean);

    const cardsMarkup = rows.length
      ? rows
          .map((brand) => `
            <article class="brand-card">
              <div class="brand-card__header">
                <div class="brand-card__avatar">${getBrandInitials(brand.name)}</div>
                <span class="brand-card__id">ID #${brand.id}</span>
              </div>
              <div class="brand-card__body">
                <h2>${brand.name}</h2>
                <p>Марка, която може да се използва в продуктовия каталог на SuperFoods.</p>
              </div>
              <div class="brand-card__actions">
                <a href="${brand.detailsHref}" class="brands-btn brands-btn--ghost">
                  <i class="bi bi-eye"></i>
                  Детайли
                </a>
                <a href="${brand.editHref}" class="brands-btn brands-btn--outline">
                  <i class="bi bi-pencil-square"></i>
                  Редакция
                </a>
                <a href="${brand.deleteHref}" class="brands-btn brands-btn--danger">
                  <i class="bi bi-trash3"></i>
                  Изтрий
                </a>
              </div>
            </article>
          `)
          .join('')
      : `
          <div class="brands-empty">
            <div class="brands-empty__icon">
              <i class="bi bi-tags"></i>
            </div>
            <h2>Все още няма добавени марки</h2>
            <p>Започни с първия бранд и изгради по-подреден каталог за продуктите.</p>
            <a href="${createLink?.getAttribute('href') ?? '/Brands/Create'}" class="brands-btn brands-btn--primary">
              <i class="bi bi-plus-circle"></i>
              Създай първа марка
            </a>
          </div>
        `;

    const alerts = Array.from(main.querySelectorAll(':scope > .alert'));

    document.title = 'Марки - SuperFoods';
    main.innerHTML = '';
    alerts.forEach((alert) => main.appendChild(alert));
    main.insertAdjacentHTML(
      'beforeend',
      `
        <section class="brands-page">
          <div class="container">
            <div class="brands-hero">
              <div class="brands-hero__content">
                <span class="brands-eyebrow">
                  <i class="bi bi-grid-1x2-fill"></i>
                  Каталог марки
                </span>
                <h1>По-чист и подреден изглед за всички брандове</h1>
                <p>
                  Управлявай марките в каталога от едно място с по-ясна структура,
                  по-добър контраст и бърз достъп до всяко действие.
                </p>
              </div>

              <div class="brands-hero__panel">
                <div class="brands-stat">
                  <span class="brands-stat__value">${rows.length}</span>
                  <span class="brands-stat__label">общо марки</span>
                </div>

                <div class="brands-hero__divider"></div>

                <p class="brands-hero__note">
                  Добавяне, преглед, редакция и изтриване без претрупана таблица.
                </p>
              </div>
            </div>

            <div class="brands-toolbar">
              <div class="brands-toolbar__meta">
                <span class="brands-toolbar__badge">
                  <i class="bi bi-stars"></i>
                  Подредени по азбучен ред
                </span>
              </div>

              <a href="${createLink?.getAttribute('href') ?? '/Brands/Create'}" class="brands-btn brands-btn--primary">
                <i class="bi bi-plus-circle"></i>
                Добави марка
              </a>
            </div>

            ${rows.length ? `<div class="brands-grid">${cardsMarkup}</div>` : cardsMarkup}
          </div>
        </section>
      `
    );
  };

  const initLegacyBrandCreatePage = () => {
    const normalizedPath = window.location.pathname.replace(/\/+$/, '').toLowerCase();

    if (normalizedPath !== '/brands/create') {
      return;
    }

    const main = document.querySelector('main[role="main"]');

    if (!main) {
      return;
    }

    if (main.querySelector('.brand-form-page')) {
      initBrandPreview(main);
      return;
    }

    const form = main.querySelector('form');
    const nameInput = form?.querySelector('input[name="Name"]');
    const label = form?.querySelector('label[for="Name"]');
    const validationSummary = form?.querySelector('div.text-danger');
    const fieldError = form?.querySelector('span.text-danger');
    const submitInput = form?.querySelector('input[type="submit"], button[type="submit"]');
    const backLink = main.querySelector('a[href$="/Brands"]');

    if (!form || !nameInput || !submitInput) {
      return;
    }

    const alerts = Array.from(main.querySelectorAll(':scope > .alert'));
    const summaryBlock = validationSummary ?? document.createElement('div');
    const errorBlock = fieldError ?? document.createElement('span');
    const secondaryAction = backLink ?? document.createElement('a');

    form.className = 'brand-form';
    summaryBlock.className = 'brand-form__summary text-danger';
    label?.classList.add('brand-form__label');

    if (label) {
      label.textContent = 'Име на марката';
    }

    nameInput.className = 'form-control brand-form__input';
    nameInput.placeholder = "Напр. Nature's Way";
    nameInput.setAttribute('data-brand-preview-input', '');

    errorBlock.className = 'brand-form__error text-danger';

    submitInput.className = 'brands-btn brands-btn--primary brand-form__submit';
    if (submitInput instanceof HTMLInputElement) {
      submitInput.value = 'Създай марка';
    } else {
      submitInput.innerHTML = '<i class="bi bi-check2-circle"></i> Създай марка';
    }

    secondaryAction.className = 'brands-btn brands-btn--ghost';
    secondaryAction.setAttribute('href', secondaryAction.getAttribute('href') || '/Brands');
    secondaryAction.innerHTML = '<i class="bi bi-arrow-left"></i> Назад към марки';

    const formGroup = document.createElement('div');
    formGroup.className = 'brand-form__group';

    const field = document.createElement('div');
    field.className = 'brand-form__field';
    field.innerHTML = '<i class="bi bi-tag"></i>';
    field.appendChild(nameInput);

    const helper = document.createElement('p');
    helper.className = 'brand-form__helper';
    helper.textContent = 'Това име ще се вижда в списъка с марки и при избор на продукт.';

    if (label) {
      formGroup.appendChild(label);
    }

    formGroup.appendChild(field);
    formGroup.appendChild(helper);
    formGroup.appendChild(errorBlock);

    const preview = document.createElement('div');
    preview.className = 'brand-form-preview';
    preview.innerHTML = `
      <span class="brand-form-preview__badge" data-brand-preview-badge>НМ</span>
      <div>
        <span class="brand-form-preview__label">Преглед</span>
        <strong class="brand-form-preview__name" data-brand-preview-text>Нова марка</strong>
        <p>Така ще изглежда записът в обновения списък с брандове.</p>
      </div>
    `;

    const actions = document.createElement('div');
    actions.className = 'brand-form__actions';
    actions.appendChild(submitInput);
    actions.appendChild(secondaryAction);

    form.replaceChildren(summaryBlock, formGroup, preview, actions);

    const section = document.createElement('section');
    section.className = 'brand-form-page';
    section.innerHTML = `
      <div class="container">
        <div class="brand-form-shell">
          <div class="brand-form-intro">
            <span class="brands-eyebrow">
              <i class="bi bi-plus-square"></i>
              Нова марка
            </span>

            <h1>Добави нов бранд в каталога</h1>
            <p>
              Създай нова марка, която след това може да се използва в продуктите,
              филтрите и административните списъци на SuperFoods.
            </p>

            <div class="brand-form-tips">
              <div class="brand-form-tip">
                <div class="brand-form-tip__icon">
                  <i class="bi bi-type"></i>
                </div>
                <div>
                  <h2>Кратко и разпознаваемо име</h2>
                  <p>Използвай официалното име на марката, за да се намира лесно в каталога.</p>
                </div>
              </div>

              <div class="brand-form-tip">
                <div class="brand-form-tip__icon">
                  <i class="bi bi-diagram-3"></i>
                </div>
                <div>
                  <h2>По-добра подредба</h2>
                  <p>Всяка нова марка се добавя към централизирания списък за по-ясно управление.</p>
                </div>
              </div>
            </div>
          </div>

          <div class="brand-form-card">
            <div class="brand-form-card__header">
              <div>
                <span class="brand-form-card__eyebrow">Форма за създаване</span>
                <h2>Детайли за марката</h2>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;

    const targetCard = section.querySelector('.brand-form-card');
    targetCard?.appendChild(form);

    document.title = 'Нова марка - SuperFoods';
    main.innerHTML = '';
    alerts.forEach((alert) => main.appendChild(alert));
    main.appendChild(section);
    initBrandPreview(main);
  };

  document.addEventListener('DOMContentLoaded', () => {
    initNavbarScrollState();
    initLegacyBrandsPage();
    initLegacyBrandCreatePage();
    initBrandPreview();
  });
})();
