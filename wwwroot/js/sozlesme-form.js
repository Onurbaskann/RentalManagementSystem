function handleBirimChange(value) {
    const card = document.getElementById('birimInfoCard');
    const text = document.getElementById('birimInfoText');

    if (value && birimler && birimler[value]) {
        const b = birimler[value];
        text.textContent = `${b.ad} • ${b.m2} • ${b.ilce}, ${b.il}`;
        card.style.display = 'block';
    } else {
        card.style.display = 'none';
    }
}

function updateBedelLabel(periyot) {
    const label = document.getElementById('bedelLabel');
    if (label) {
        label.textContent = periyot === 2 ? 'Yıllık Bedel (₺) *' : 'Aylık Bedel (₺) *';
    }
}

function validateDates() {
    const baslangic = document.getElementById('baslangicInput');
    const bitis = document.getElementById('bitisInput');
    const errDiv = document.getElementById('dateError');
    const submitBtn = document.getElementById('submitBtn');

    if (baslangic && bitis && errDiv) {
        const ok = !bitis.value || !baslangic.value || bitis.value > baslangic.value;
        errDiv.style.display = ok ? 'none' : 'block';
        if (submitBtn) submitBtn.disabled = !ok;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    validateDates();
    const birimSelect = document.getElementById('birimSelect');
    if (birimSelect && birimSelect.value) handleBirimChange(birimSelect.value);
});
