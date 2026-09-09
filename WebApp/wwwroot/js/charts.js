window.renderPriceChart = function (data) {

    const canvas = document.getElementById('precoChart') || document.getElementById('priceChart');
    if (!canvas) {
        console.error("[ERROR] Elemento canvas não encontrado.");
        return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error("[ERROR] Não foi possível obter o contexto 2D do canvas.");
        return;
    }

    // Destruir qualquer gráfico existente para evitar conflitos
    if (window.activeChart) {
        window.activeChart.destroy();
    }

    // Formatar as datas como strings no formato DD/MM/YYYY
    const labels = [];
    const datasets = data.map((item, index) => {
        const prices = item.prices.map(price => {
            const date = new Date(price.date);
            const formattedDate = `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
            if (!labels.includes(formattedDate)) {
                labels.push(formattedDate);
            }
            return {
                x: formattedDate,
                y: price.price
            };
        });

        return {
            label: item.lojaNome,
            data: prices,
            borderColor: getColor(index),
            backgroundColor: getColor(index, 0.2),
            fill: false,
            tension: 0.1,
            pointRadius: 5, // Mostrar pontos visíveis
            pointHoverRadius: 8, // Aumentar o tamanho do ponto ao passar o rato
            showLine: true // Garantir que a linha é desenhada entre os pontos
        };
    });

    // Ordenar as labels por data para garantir que o eixo X esteja em ordem cronológica
    labels.sort((a, b) => {
        const [dayA, monthA, yearA] = a.split('/').map(Number);
        const [dayB, monthB, yearB] = b.split('/').map(Number);
        const dateA = new Date(yearA, monthA - 1, dayA);
        const dateB = new Date(yearB, monthB - 1, dayB);
        return dateA - dateB;
    });

    window.activeChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Data'
                    },
                    ticks: {
                        maxRotation: 45,
                        minRotation: 45
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Preço (€)'
                    },
                    beginAtZero: true
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'right', // Mover a legenda para a lateral direita
                    labels: {
                        boxWidth: 20,
                        padding: 15
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: function (context) {
                            return `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`;
                        }
                    }
                }
            }
        }
    });

};

function getColor(index, alpha = 1) {
    const colors = [
        `rgba(75, 192, 192, ${alpha})`, // Teal
        `rgba(255, 99, 132, ${alpha})`,  // Red
        `rgba(54, 162, 235, ${alpha})`,  // Blue
        `rgba(255, 206, 86, ${alpha})`,  // Yellow
        `rgba(153, 102, 255, ${alpha})`  // Purple
    ];
    return colors[index % colors.length];
}
