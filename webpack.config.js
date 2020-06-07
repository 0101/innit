// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

var isProduction = !hasArg(/webpack-dev-server/);

module.exports = [
    {
        mode: isProduction ? 'production' : 'development',
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        entry: "./src/App.fsproj",
        output: {
            path: path.join(__dirname, "./public"),
            filename: "bundle.js",
        },
        devServer: {
            publicPath: "/",
            contentBase: "./public",
            port: 8080,
            host: '192.168.1.217',
            allowedHosts: [
                'localhost',
                '0.0.0.0',
                '192.168.1.217'
            ]
        },
        module: {
            rules: [{
                test: /\.fs(x|proj)?$/,
                use: "fable-loader"
            }]
        }
    },
    {
        entry: "./src/Solver.fs",
        output: {
            path: path.join(__dirname, './public/Workers'),
            filename: "Solver.js",
            library: "Solver",
            libraryTarget: "umd",
        },
        mode: isProduction ? 'production' : 'development',
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        resolve: {
            symlinks: false
        },
        module: {
            rules: [
                {
                    test: /\.fs(x|proj)?$/,
                    use: {
                        loader: 'fable-loader',
                        options: {
                            allFiles: true,
                            define: []
                        }
                    }
                },
                {
                    test: /\.js$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader'
                    }
                }
            ]
        },
        target: 'webworker'
    }
]


function hasArg(arg) {
    return arg instanceof RegExp
        ? process.argv.some(x => arg.test(x))
        : process.argv.indexOf(arg) !== -1;
}